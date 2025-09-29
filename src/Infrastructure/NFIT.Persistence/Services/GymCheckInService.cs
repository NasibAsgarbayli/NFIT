using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NFIT.Application.Abstracts.Services;
using NFIT.Application.DTOs.GymChechkInDtos;
using NFIT.Application.Shared;
using NFIT.Domain.Entities;
using NFIT.Domain.Enums;
using NFIT.Persistence.Contexts;

namespace NFIT.Persistence.Services;

public class GymCheckInService:IGymCheckInService
{
    private readonly NFITDbContext _context;
    private readonly IHttpContextAccessor _http;

    public GymCheckInService(NFITDbContext ctx, IHttpContextAccessor http)
    {
        _context = ctx;
        _http = http;
    }

    private string? UserId =>
        _http.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? _http.HttpContext?.User?.FindFirst("sub")?.Value;

    public async Task<BaseResponse<Guid>> CheckInAsync(CheckInRequestDto dto)
    {
        if (dto == null)
            return new BaseResponse<Guid>("Request body is required", Guid.Empty, HttpStatusCode.BadRequest);

        if (string.IsNullOrWhiteSpace(UserId))
            return new BaseResponse<Guid>("Unauthorized", Guid.Empty, HttpStatusCode.Unauthorized);

        if (string.IsNullOrWhiteSpace(dto.QrData))
            return new BaseResponse<Guid>("QR data is required", Guid.Empty, HttpStatusCode.BadRequest);

        // 1) QR tap
        var qr = await _context.GymQRCodes
            .FirstOrDefaultAsync(q => q.QRCodeData == dto.QrData && q.IsActive && !q.IsDeleted);
        if (qr is null)
            return new BaseResponse<Guid>("Invalid or inactive QR", Guid.Empty, HttpStatusCode.BadRequest);

        // 2) Aktiv check-in yoxla
        var hasActive = await _context.GymCheckIns.AnyAsync(c =>
            c.UserId == UserId && c.Status == CheckInStatus.Active && !c.IsDeleted);
        if (hasActive)
            return new BaseResponse<Guid>("You already have an active check-in", Guid.Empty, HttpStatusCode.Conflict);

        // 3) İstifadəçinin aktiv üzvlüyü var?
        var now = DateTime.UtcNow;
        var gym = await _context.Gyms
            .Include(g => g.AvailableSubscriptions)
            .FirstOrDefaultAsync(g => g.Id == qr.GymId && !g.IsDeleted && g.IsActive);
        if (gym is null)
            return new BaseResponse<Guid>("Gym not found or inactive", Guid.Empty, HttpStatusCode.NotFound);

        var availablePlanIds = gym.AvailableSubscriptions
            .Where(p => !p.IsDeleted)
            .Select(p => p.Id)
            .ToHashSet();

        var hasValidMembership = await _context.Memberships
            .AnyAsync(m =>
                m.UserId == UserId &&
                m.IsActive &&
                m.StartDate <= now && now < m.EndDate &&
                !m.IsDeleted &&
                availablePlanIds.Contains(m.SubscriptionPlanId));

        if (!hasValidMembership)
            return new BaseResponse<Guid>(
                "You have no active membership valid for this gym",
                Guid.Empty,
                HttpStatusCode.Forbidden);

        // 4) Check-in yarat
        var ci = new GymCheckIn
        {
            Id = Guid.NewGuid(),
            GymId = qr.GymId,
            UserId = UserId!,
            CheckInTime = now,
            Status = CheckInStatus.Active,   // enum dəyəri backend tərəfindən set olunur
            Notes = dto.Notes
        };

        await _context.GymCheckIns.AddAsync(ci);
        await _context.SaveChangesAsync();

        return new BaseResponse<Guid>("Checked in", ci.Id, HttpStatusCode.Created);
    }

    public async Task<BaseResponse<string>> CheckOutAsync(CheckOutRequestDto dto)
    {
        if (dto == null)
            return new BaseResponse<string>("Request body is required", HttpStatusCode.BadRequest);

        if (string.IsNullOrWhiteSpace(UserId))
            return new BaseResponse<string>("Unauthorized", HttpStatusCode.Unauthorized);

        if (dto.CheckInId == Guid.Empty)
            return new BaseResponse<string>("Check-in id is required", HttpStatusCode.BadRequest);

        var ci = await _context.GymCheckIns
            .FirstOrDefaultAsync(c => c.Id == dto.CheckInId && !c.IsDeleted);

        if (ci is null)
            return new BaseResponse<string>("Check-in not found", HttpStatusCode.NotFound);

        if (ci.UserId != UserId)
            return new BaseResponse<string>("Forbidden", HttpStatusCode.Forbidden);

        if (ci.Status != CheckInStatus.Active)
            return new BaseResponse<string>("Already closed", HttpStatusCode.BadRequest);

        ci.CheckOutTime = DateTime.UtcNow;
        ci.Status = CheckInStatus.CheckedOut;   // enum dəyəri burada da backend tərəfindən təyin olunur
        ci.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return new BaseResponse<string>("Checked out", HttpStatusCode.OK);
    }

    public async Task<BaseResponse<CheckInGetDto?>> GetMyActiveAsync()
    {
        if (string.IsNullOrWhiteSpace(UserId))
            return new BaseResponse<CheckInGetDto?>("Unauthorized", null, HttpStatusCode.Unauthorized);

        var c = await _context.GymCheckIns
            .Include(x => x.Gym)
            .Where(x => x.UserId == UserId && x.Status == CheckInStatus.Active && !x.IsDeleted)
            .OrderByDescending(x => x.CheckInTime)
            .FirstOrDefaultAsync();

        if (c is null)
            return new BaseResponse<CheckInGetDto?>("No active check-in", null, HttpStatusCode.NotFound);

        return new BaseResponse<CheckInGetDto?>("Active check-in", new CheckInGetDto
        {
            Id = c.Id,
            GymId = c.GymId,
            GymName = c.Gym.Name!,
            CheckInTime = c.CheckInTime,
            CheckOutTime = c.CheckOutTime,
            Status = c.Status.ToString(),
            Duration = c.Duration
        }, HttpStatusCode.OK);
    }

    public async Task<BaseResponse<List<CheckInGetDto>>> GetMyHistoryAsync(int days = 30)
    {
        if (string.IsNullOrWhiteSpace(UserId))
            return new BaseResponse<List<CheckInGetDto>>("Unauthorized", null, HttpStatusCode.Unauthorized);

        var since = DateTime.UtcNow.AddDays(-Math.Abs(days));
        var list = await _context.GymCheckIns
            .Include(x => x.Gym)
            .Where(x => x.UserId == UserId && !x.IsDeleted && x.CheckInTime >= since)
            .OrderByDescending(x => x.CheckInTime)
            .Select(x => new CheckInGetDto
            {
                Id = x.Id,
                GymId = x.GymId,
                GymName = x.Gym.Name!,
                CheckInTime = x.CheckInTime,
                CheckOutTime = x.CheckOutTime,
                Status = x.Status.ToString(),
                Duration = x.Duration
            })
            .ToListAsync();

        if (list.Count == 0)
            return new BaseResponse<List<CheckInGetDto>>("No history", null, HttpStatusCode.NotFound);

        return new BaseResponse<List<CheckInGetDto>>("History retrieved", list, HttpStatusCode.OK);
    }

    public async Task<BaseResponse<int>> GetCurrentOccupancyAsync(Guid gymId)
    {
        var count = await _context.GymCheckIns
            .CountAsync(c => c.GymId == gymId &&
                             c.Status == CheckInStatus.Active &&   // ✅ yeni enum adı
                             !c.IsDeleted);
        return new BaseResponse<int>("Occupancy retrieved", count, HttpStatusCode.OK);
    }
}
