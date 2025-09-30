using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NFIT.Application.Abstracts.Services;
using NFIT.Application.DTOs.MembershipDtos;
using NFIT.Application.Shared;
using NFIT.Persistence.Contexts;
using static NFIT.Application.Shared.Permissions;

namespace NFIT.Persistence.Services;

public class MembershipService:IMembershipService
{
    private readonly NFITDbContext _context;
    private readonly IHttpContextAccessor _http;

    public MembershipService(NFITDbContext context, IHttpContextAccessor http)
    {
        _context = context;
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
            return new BaseResponse<Guid>("Unauthorized", Guid.Empty, HttpStatusCode.Unauthorized);

        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            // Order user-ə məxsus, silinməyib, subscription order və Delivered olmalıdır
            var order = await _context.Orders
                .Include(o => o.SubscriptionPlan)
                .FirstOrDefaultAsync(o =>
                    o.Id == dto.OrderId &&
                    o.UserId == userId &&
                    !o.IsDeleted);

            if (order is null)
                return new BaseResponse<Guid>("Order not found", Guid.Empty, HttpStatusCode.NotFound);

            if (order.SubscriptionPlanId is null)
                return new BaseResponse<Guid>("Order is not a subscription order", Guid.Empty, HttpStatusCode.BadRequest);

            if (order.Status != Domain.Enums.SupplementOrderStatus.Delivered)
                return new BaseResponse<Guid>("Order is not delivered", Guid.Empty, HttpStatusCode.BadRequest);

            // eyni order ikinci dəfə istifadə olunmasın
            if (order.ConsumedByMembershipId is not null)
                return new BaseResponse<Guid>(
                    "This order has already been used to create a membership",
                    order.ConsumedByMembershipId.Value,
                    HttpStatusCode.Conflict);

            // istəyə görə siyasət: eyni anda yalnız 1 aktiv membership
            var now = DateTime.UtcNow;
            var current = await _context.Memberships
                .Where(m => m.UserId == userId && m.IsActive && !m.IsDeleted && m.EndDate > now)
                .OrderByDescending(m => m.StartDate)
                .FirstOrDefaultAsync();

            // Variant A: mövcud aktiv membership-i dayandır
            if (current is not null)
            {
                current.IsActive = false;
                current.EndDate = now;
                current.UpdatedAt = now;
                _context.Memberships.Update(current);
                await _context.SaveChangesAsync();
            }
            // Variant B (əvəzinə Conflict qaytarmaq istəsən):
            // if (current is not null)
            //     return new BaseResponse<Guid>("An active membership already exists", Guid.Empty, HttpStatusCode.Conflict);

            var plan = order.SubscriptionPlan!;
            var start = DateTime.UtcNow;
            var end = plan.BillingCycle switch
            {
                Domain.Enums.BillingCycle.Monthly => start.AddMonths(1),
                Domain.Enums.BillingCycle.Yearly => start.AddYears(1),
                _ => start.AddMonths(1)
            };

            var membership = new Domain.Entities.Membership
            {
                Id = Guid.NewGuid(),
                UserId = userId!,
                SubscriptionPlanId = plan.Id,
                StartDate = start,
                EndDate = end,
                IsActive = true,
                CreatedAt = now
            };

            await _context.Memberships.AddAsync(membership);
            await _context.SaveChangesAsync();

            // order-u consumed kimi işarələ (idempotency)
            order.ConsumedByMembershipId = membership.Id;
            order.ConsumedForMembershipAt = DateTime.UtcNow;
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();

            await tx.CommitAsync();

            return new BaseResponse<Guid>(
                "Membership created from delivered order",
                membership.Id,
                HttpStatusCode.Created);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // ============ MY: CURRENT (aktiv yoxdursa ən son) ============
    public async Task<BaseResponse<MembershipGetDto>> GetMyMembershipAsync()
    {
        if (string.IsNullOrWhiteSpace(CurrentUserId))
            return new BaseResponse<MembershipGetDto>("Unauthorized", null, HttpStatusCode.Unauthorized);

        var m = await _context.Memberships
            .AsNoTracking()
            .Include(x => x.SubscriptionPlan)
            .Where(x => x.UserId == CurrentUserId && !x.IsDeleted)
            .OrderByDescending(x => x.IsActive)
            .ThenByDescending(x => x.StartDate)
            .FirstOrDefaultAsync();

        if (m is null)
            return new BaseResponse<MembershipGetDto>("No membership found", null, HttpStatusCode.NotFound);

        return new BaseResponse<MembershipGetDto>("Membership retrieved", Map(m), HttpStatusCode.OK);
    }

    // ============ MY: HISTORY ============
    public async Task<BaseResponse<List<MembershipListItemDto>>> GetMyMembershipHistoryAsync()
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
            return new BaseResponse<List<MembershipListItemDto>>("Unauthorized", null, HttpStatusCode.Unauthorized);

        var list = await _context.Memberships
            .AsNoTracking()
            .Include(x => x.SubscriptionPlan)
            .Where(x => x.UserId == userId && !x.IsDeleted)
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
            return new BaseResponse<List<MembershipListItemDto>>("No membership history", null, HttpStatusCode.NotFound);

        return new BaseResponse<List<MembershipListItemDto>>("Membership history retrieved", list, HttpStatusCode.OK);
    }

    // ============ MY: CANCEL (derhal) ============
    public async Task<BaseResponse<string>> CancelMyMembershipAsync()
    {
        if (string.IsNullOrWhiteSpace(CurrentUserId))
            return new BaseResponse<string>("Unauthorized", HttpStatusCode.Unauthorized);

        var now = DateTime.UtcNow;

        var m = await _context.Memberships
            .FirstOrDefaultAsync(x =>
                x.UserId == CurrentUserId &&
                x.IsActive &&
                !x.IsDeleted &&
                x.EndDate > now);

        if (m is null)
            return new BaseResponse<string>("Active membership not found", HttpStatusCode.NotFound);

        m.IsActive = false;
        m.EndDate = now;
        m.UpdatedAt = now;

        _context.Memberships.Update(m);
        await _context.SaveChangesAsync();

        return new BaseResponse<string>("Membership cancelled", HttpStatusCode.OK);
    }

    // ============ MY: DELETE (soft) ============
    public async Task<BaseResponse<string>> DeleteAsync(Guid id)
    {
        if (string.IsNullOrWhiteSpace(CurrentUserId))
            return new BaseResponse<string>("Unauthorized", HttpStatusCode.Unauthorized);

        var m = await _context.Memberships
            .Include(x => x.SubscriptionPlan)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (m is null)
            return new BaseResponse<string>("Membership not found", HttpStatusCode.NotFound);

        if (m.UserId != CurrentUserId)
            return new BaseResponse<string>("Forbidden", HttpStatusCode.Forbidden);

        m.IsDeleted = true;
        m.IsActive = false;
        m.UpdatedAt = DateTime.UtcNow;

        _context.Memberships.Update(m);
        await _context.SaveChangesAsync();

        return new BaseResponse<string>("Membership deleted (soft)", HttpStatusCode.OK);
    }

    // ============ ADMIN: istənilən user-in hazırkı membership-i ============
    public async Task<BaseResponse<MembershipGetDto>> GetUsersMembershipAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return new BaseResponse<MembershipGetDto>("User id is required", null, HttpStatusCode.BadRequest);

        var userExists = await _context.Users.AnyAsync(u => u.Id == userId && u.IsActive);
        if (!userExists)
            return new BaseResponse<MembershipGetDto>("User not found", null, HttpStatusCode.NotFound);

        var m = await _context.Memberships
            .AsNoTracking()
            .Include(x => x.SubscriptionPlan)
            .Where(x => x.UserId == userId && !x.IsDeleted)
            .OrderByDescending(x => x.IsActive)
            .ThenByDescending(x => x.StartDate)
            .FirstOrDefaultAsync();

        if (m is null)
            return new BaseResponse<MembershipGetDto>("No membership found for user", null, HttpStatusCode.NotFound);

        return new BaseResponse<MembershipGetDto>("User membership retrieved", Map(m), HttpStatusCode.OK);
    }

    // ============ ADMIN: deactivate ============
    public async Task<BaseResponse<string>> DeactivateUserMembershipAsync(string userId)
    {
        var now = DateTime.UtcNow;

        var m = await _context.Memberships
            .Where(x => x.UserId == userId && x.IsActive && !x.IsDeleted && x.EndDate > now)
            .OrderByDescending(x => x.StartDate)
            .FirstOrDefaultAsync();

        if (m is null)
            return new BaseResponse<string>("Active membership not found for user", HttpStatusCode.NotFound);

        m.IsActive = false;
        m.EndDate = now;
        m.UpdatedAt = now;

        _context.Memberships.Update(m);
        await _context.SaveChangesAsync();

        return new BaseResponse<string>("User membership deactivated", HttpStatusCode.OK);
    }

    // ============ ACCESS CHECK: müəyyən gym üçün aktiv membership ============
    public async Task<bool> HasActiveMembershipForGymAsync(string userId, Guid gymId)
    {
        var now = DateTime.UtcNow;

        var planIds = await _context.Gyms
            .Where(g => g.Id == gymId && g.IsActive && !g.IsDeleted)
            .SelectMany(g => g.AvailableSubscriptions.Where(p => !p.IsDeleted))
            .Select(p => p.Id)
            .ToListAsync();

        if (planIds.Count == 0) return false;

        return await _context.Memberships.AnyAsync(m =>
            m.UserId == userId &&
            m.IsActive && !m.IsDeleted &&
            m.StartDate <= now && now < m.EndDate &&
            planIds.Contains(m.SubscriptionPlanId));
    }

    // ============ MAP helper ============
    private static MembershipGetDto Map(Domain.Entities.Membership m) => new()
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

