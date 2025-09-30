using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NFIT.Application.Abstracts.Repositories;
using NFIT.Application.Abstracts.Services;
using NFIT.Application.DTOs.MembershipDtos;
using NFIT.Application.Shared;
using NFIT.Domain.Entities;
using NFIT.Persistence.Contexts;
using static NFIT.Application.Shared.Permissions.Membership;

namespace NFIT.Persistence.Services;

public class MembershipService:IMembershipService
{
    private readonly IMembershipRepository _memberships;
    private readonly IOrderRepository _orders;
    private readonly IGymRepository _gyms;
    private readonly ISubscriptionPlanRepository _plans;
    private readonly UserManager<AppUser> _userManager;
    private readonly IHttpContextAccessor _http;

    public MembershipService(
        IMembershipRepository memberships,
        IOrderRepository orders,
        IGymRepository gyms,
        ISubscriptionPlanRepository plans,
        UserManager<AppUser> userManager,
        IHttpContextAccessor http)
    {
        _memberships = memberships;
        _orders = orders;
        _gyms = gyms;
        _plans = plans;
        _userManager = userManager;
        _http = http;
    }

    private string? CurrentUserId =>
        _http.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? _http.HttpContext?.User?.FindFirst("sub")?.Value;

    // ============ CREATE from Delivered Subscription Order ============
    public async Task<BaseResponse<Guid>> CreateFromDeliveredOrderAsync(MembershipCreateFromOrderDto dto)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
            return new("Unauthorized", Guid.Empty, HttpStatusCode.Unauthorized);

        // Order user-ə məxsus, subscription order və Delivered olmalıdır
        var order = await _orders
            .GetByFiltered(o => o.Id == dto.OrderId && o.UserId == userId && !o.IsDeleted,
                           include: new[] { (System.Linq.Expressions.Expression<Func<Order, object>>)(o => o.SubscriptionPlan) })
            .FirstOrDefaultAsync();

        if (order is null)
            return new("Order not found", Guid.Empty, HttpStatusCode.NotFound);

        if (order.SubscriptionPlanId is null)
            return new("Order is not a subscription order", Guid.Empty, HttpStatusCode.BadRequest);

        if (order.Status != NFIT.Domain.Enums.SupplementOrderStatus.Delivered)
            return new("Order is not delivered", Guid.Empty, HttpStatusCode.BadRequest);

        if (order.ConsumedByMembershipId is not null)
            return new("This order has already been used to create a membership",
                       order.ConsumedByMembershipId.Value, HttpStatusCode.Conflict);

        var now = DateTime.UtcNow;

        // siyasət: eyni anda yalnız 1 aktiv membership → varsa dayandır
        var current = await _memberships
            .GetByFiltered(m => m.UserId == userId && m.IsActive && !m.IsDeleted && m.EndDate > now)
            .OrderByDescending(m => m.StartDate)
            .FirstOrDefaultAsync();

        if (current is not null)
        {
            current.IsActive = false;
            current.EndDate = now;
            current.UpdatedAt = now;
            _memberships.Update(current);
        }

        var plan = order.SubscriptionPlan!;
        var start = now;
        var end = plan.BillingCycle switch
        {
            NFIT.Domain.Enums.BillingCycle.Monthly => start.AddMonths(1),
            NFIT.Domain.Enums.BillingCycle.Yearly => start.AddYears(1),
            _ => start.AddMonths(1)
        };

        var membership = new Membership
        {
            Id = Guid.NewGuid(),
            UserId = userId!,
            SubscriptionPlanId = plan.Id,
            StartDate = start,
            EndDate = end,
            IsActive = true,
            CreatedAt = now
        };

        await _memberships.AddAsync(membership);

        // order-u consumed kimi işarələ
        order.ConsumedByMembershipId = membership.Id;
        order.ConsumedForMembershipAt = now;
        _orders.Update(order);

        // hamısını BİRDƏFƏLİK yaz
        await _memberships.SaveChangeAsync();

        return new("Membership created from delivered order", membership.Id, HttpStatusCode.Created);
    }

    // ============ MY: CURRENT (aktiv yoxdursa ən son) ============
    public async Task<BaseResponse<MembershipGetDto>> GetMyMembershipAsync()
    {
        if (string.IsNullOrWhiteSpace(CurrentUserId))
            return new("Unauthorized", null, HttpStatusCode.Unauthorized);

        var m = await _memberships
            .GetByFiltered(x => x.UserId == CurrentUserId && !x.IsDeleted,
                           include: new[] { (System.Linq.Expressions.Expression<Func<Membership, object>>)(x => x.SubscriptionPlan) },
                           IsTracking: false)
            .OrderByDescending(x => x.IsActive)
            .ThenByDescending(x => x.StartDate)
            .FirstOrDefaultAsync();

        if (m is null)
            return new("No membership found", null, HttpStatusCode.NotFound);

        return new("Membership retrieved", Map(m), HttpStatusCode.OK);
    }

    // ============ MY: HISTORY ============
    public async Task<BaseResponse<List<MembershipListItemDto>>> GetMyMembershipHistoryAsync()
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
            return new("Unauthorized", null, HttpStatusCode.Unauthorized);

        var list = await _memberships
            .GetByFiltered(x => x.UserId == userId && !x.IsDeleted,
                           include: new[] { (System.Linq.Expressions.Expression<Func<Membership, object>>)(x => x.SubscriptionPlan) },
                           IsTracking: false)
            .OrderByDescending(x => x.StartDate)
            .Select(x => new MembershipListItemDto
            {
                Id = x.Id,
                PlanName = x.SubscriptionPlan!.Name,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                IsActive = x.IsActive
            })
            .ToListAsync();

        if (list.Count == 0)
            return new("No membership history", null, HttpStatusCode.NotFound);

        return new("Membership history retrieved", list, HttpStatusCode.OK);
    }

    // ============ MY: CANCEL (derhal) ============
    public async Task<BaseResponse<string>> CancelMyMembershipAsync()
    {
        if (string.IsNullOrWhiteSpace(CurrentUserId))
            return new("Unauthorized", HttpStatusCode.Unauthorized);

        var now = DateTime.UtcNow;

        var m = await _memberships
            .GetByFiltered(x => x.UserId == CurrentUserId && x.IsActive && !x.IsDeleted && x.EndDate > now)
            .OrderByDescending(x => x.StartDate)
            .FirstOrDefaultAsync();

        if (m is null)
            return new("Active membership not found", HttpStatusCode.NotFound);

        m.IsActive = false;
        m.EndDate = now;
        m.UpdatedAt = now;

        _memberships.Update(m);
        await _memberships.SaveChangeAsync();

        return new("Membership cancelled", HttpStatusCode.OK);
    }

    // ============ MY: DELETE (soft) ============
    public async Task<BaseResponse<string>> DeleteAsync(Guid id)
    {
        if (string.IsNullOrWhiteSpace(CurrentUserId))
            return new("Unauthorized", HttpStatusCode.Unauthorized);

        var m = await _memberships
            .GetByFiltered(x => x.Id == id && !x.IsDeleted,
                           include: new[] { (System.Linq.Expressions.Expression<Func<Membership, object>>)(x => x.SubscriptionPlan) })
            .FirstOrDefaultAsync();

        if (m is null)
            return new("Membership not found", HttpStatusCode.NotFound);

        if (m.UserId != CurrentUserId)
            return new("Forbidden", HttpStatusCode.Forbidden);

        m.IsDeleted = true;
        m.IsActive = false;
        m.UpdatedAt = DateTime.UtcNow;

        _memberships.Update(m);
        await _memberships.SaveChangeAsync();

        return new("Membership deleted (soft)", HttpStatusCode.OK);
    }

    // ============ ADMIN: istənilən user-in hazırkı membership-i ============
    public async Task<BaseResponse<MembershipGetDto>> GetUsersMembershipAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return new("User id is required", null, HttpStatusCode.BadRequest);

        var userExists = await _userManager.Users.AnyAsync(u => u.Id == userId && u.IsActive);
        if (!userExists)
            return new("User not found", null, HttpStatusCode.NotFound);

        var m = await _memberships
            .GetByFiltered(x => x.UserId == userId && !x.IsDeleted,
                           include: new[] { (System.Linq.Expressions.Expression<Func<Membership, object>>)(x => x.SubscriptionPlan) },
                           IsTracking: false)
            .OrderByDescending(x => x.IsActive)
            .ThenByDescending(x => x.StartDate)
            .FirstOrDefaultAsync();

        if (m is null)
            return new("No membership found for user", null, HttpStatusCode.NotFound);

        return new("User membership retrieved", Map(m), HttpStatusCode.OK);
    }

    // ============ ADMIN: deactivate ============
    public async Task<BaseResponse<string>> DeactivateUserMembershipAsync(string userId)
    {
        var now = DateTime.UtcNow;

        var m = await _memberships
            .GetByFiltered(x => x.UserId == userId && x.IsActive && !x.IsDeleted && x.EndDate > now)
            .OrderByDescending(x => x.StartDate)
            .FirstOrDefaultAsync();

        if (m is null)
            return new("Active membership not found for user", HttpStatusCode.NotFound);

        m.IsActive = false;
        m.EndDate = now;
        m.UpdatedAt = now;

        _memberships.Update(m);
        await _memberships.SaveChangeAsync();

        return new("User membership deactivated", HttpStatusCode.OK);
    }

    // ============ ACCESS CHECK: müəyyən gym üçün aktiv membership ============
    public async Task<bool> HasActiveMembershipForGymAsync(string userId, Guid gymId)
    {
        var now = DateTime.UtcNow;

        var planIds = await _gyms
            .GetByFiltered(g => g.Id == gymId && g.IsActive && !g.IsDeleted,
                           include: new[] { (System.Linq.Expressions.Expression<Func<Gym, object>>)(g => g.AvailableSubscriptions) },
                           IsTracking: false)
            .SelectMany(g => g.AvailableSubscriptions.Where(p => !p.IsDeleted).Select(p => p.Id))
            .ToListAsync();

        if (planIds.Count == 0) return false;

        return await _memberships
            .GetByFiltered(m => m.UserId == userId &&
                                m.IsActive && !m.IsDeleted &&
                                m.StartDate <= now && now < m.EndDate &&
                                planIds.Contains(m.SubscriptionPlanId),
                           IsTracking: false)
            .AnyAsync();
    }

    // ============ MAP helper ============
    private static MembershipGetDto Map(Membership m) => new()
    {
        Id = m.Id,
        PlanName = m.SubscriptionPlan?.Name ?? "",
        PlanType = m.SubscriptionPlan?.Type.ToString() ?? "",
        BillingCycle = m.SubscriptionPlan?.BillingCycle.ToString() ?? "",
        Price = m.SubscriptionPlan?.Price ?? 0m,
        StartDate = m.StartDate,
        EndDate = m.EndDate,
        IsActive = m.IsActive
    };
}

