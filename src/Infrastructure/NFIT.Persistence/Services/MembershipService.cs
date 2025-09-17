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

    // ============ MY: CURRENT (aktiv yoxdursa ən son) ============
    public async Task<BaseResponse<MembershipGetDto>> GetMyMembershipAsync()
    {
        if (string.IsNullOrWhiteSpace(CurrentUserId))
            return new BaseResponse<MembershipGetDto>("Unauthorized", null, HttpStatusCode.Unauthorized);

        var m = await _context.Memberships
            .Include(x => x.SubscriptionPlan)
            .Where(x => x.UserId == CurrentUserId && !x.IsDeleted)
            .OrderByDescending(x => x.IsActive)
            .ThenByDescending(x => x.StartDate)
            .FirstOrDefaultAsync();

        if (m is null)
            return new BaseResponse<MembershipGetDto>("No membership found", null, HttpStatusCode.NotFound);

        return new BaseResponse<MembershipGetDto>(
            "Membership retrieved",
            Map(m),
            HttpStatusCode.OK
        );
    }
    public async Task<BaseResponse<List<MembershipListItemDto>>> GetMyMembershipHistoryAsync()
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
            return new BaseResponse<List<MembershipListItemDto>>("Unauthorized", null, HttpStatusCode.Unauthorized);

        var list = await _context.Memberships
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



    // ============ MY: CANCEL (derhal dayandır) ============
    public async Task<BaseResponse<string>> CancelMyMembershipAsync()
    {
        if (string.IsNullOrWhiteSpace(CurrentUserId))
            return new BaseResponse<string>("Unauthorized", HttpStatusCode.Unauthorized);

        var now = DateTime.UtcNow;

        var m = await _context.Memberships
            .Where(x => x.UserId == CurrentUserId && x.IsActive && !x.IsDeleted && x.EndDate > now)
            .OrderByDescending(x => x.StartDate)
            .FirstOrDefaultAsync();

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

    // ============ ADMIN: istənilən user-in membership-i ============
    public async Task<BaseResponse<MembershipGetDto>> GetUsersMembershipAsync(string userId)
    {
        // Admin policy-ni controller səviyyəsində tətbiq edəcəksən
        var m = await _context.Memberships
            .Include(x => x.SubscriptionPlan)
            .Where(x => x.UserId == userId && !x.IsDeleted)
            .OrderByDescending(x => x.IsActive)
            .ThenByDescending(x => x.StartDate)
            .FirstOrDefaultAsync();

        if (m is null)
            return new BaseResponse<MembershipGetDto>("No membership found for user", null, HttpStatusCode.NotFound);

        return new BaseResponse<MembershipGetDto>(
            "User membership retrieved",
            Map(m),
            HttpStatusCode.OK
        );
    }
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
    // ---------- map helper ----------
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

