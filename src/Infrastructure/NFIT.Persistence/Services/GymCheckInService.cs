using System.Linq.Expressions;
using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NFIT.Application.Abstracts.Repositories;
using NFIT.Application.Abstracts.Services;
using NFIT.Application.DTOs.GymChechkInDtos;
using NFIT.Application.Shared;
using NFIT.Domain.Entities;
using NFIT.Domain.Enums;
using NFIT.Persistence.Contexts;

namespace NFIT.Persistence.Services;

public class GymCheckInService:IGymCheckInService
{
    private readonly IGymCheckInRepository _checkins;
    private readonly IGymQrCodeRepository _qrRepo;
    private readonly IGymRepository _gyms;
    private readonly IHttpContextAccessor _http;
    private readonly IMembershipService _memberships;

    public GymCheckInService(
        IGymCheckInRepository checkins,
        IGymQrCodeRepository qrRepo,
        IGymRepository gyms,
        IHttpContextAccessor http,
        IMembershipService memberships)
    {
        _checkins = checkins;
        _qrRepo = qrRepo;
        _gyms = gyms;
        _http = http;
        _memberships = memberships;
    }

    private string? UserId =>
        _http.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? _http.HttpContext?.User?.FindFirst("sub")?.Value;

    // ============== CHECK IN ==============
    public async Task<BaseResponse<Guid>> CheckInAsync(CheckInRequestDto dto)
    {
        if (dto == null)
            return new BaseResponse<Guid>("Request body is required", Guid.Empty, HttpStatusCode.BadRequest);
        if (string.IsNullOrWhiteSpace(UserId))
            return new BaseResponse<Guid>("Unauthorized", Guid.Empty, HttpStatusCode.Unauthorized);
        if (string.IsNullOrWhiteSpace(dto.QrData))
            return new BaseResponse<Guid>("QR data is required", Guid.Empty, HttpStatusCode.BadRequest);

        var now = DateTime.UtcNow;

        // 1) QR
        var qr = await _qrRepo
            .GetAllFiltered(q => q.QRCodeData == dto.QrData && q.IsActive && !q.IsDeleted, IsTracking: true)
            .FirstOrDefaultAsync();

        if (qr is null || (qr.ExpiresAt != null && qr.ExpiresAt <= now))
            return new BaseResponse<Guid>("Invalid or expired QR", Guid.Empty, HttpStatusCode.BadRequest);

        if (qr.IsOneTime && qr.UsedAt != null)
            return new BaseResponse<Guid>("QR already used", Guid.Empty, HttpStatusCode.BadRequest);

        // 1.1) Gym aktivdir?
        var gymOk = await _gyms
            .GetAllFiltered(g => g.Id == qr.GymId && g.IsActive && !g.IsDeleted)
            .AnyAsync();
        if (!gymOk)
            return new BaseResponse<Guid>("Gym is not available", Guid.Empty, HttpStatusCode.BadRequest);

        // 2) Aktiv check-in varsa blokla
        var hasActive = await _checkins
            .GetAllFiltered(c => c.UserId == UserId && c.Status == CheckInStatus.Active && !c.IsDeleted)
            .AnyAsync();
        if (hasActive)
            return new BaseResponse<Guid>("You already have an active check-in", Guid.Empty, HttpStatusCode.Conflict);

        // 3) Üzvlük gym üçün keçərlidir?
        if (!await _memberships.HasActiveMembershipForGymAsync(UserId!, qr.GymId))
            return new BaseResponse<Guid>("You have no active membership valid for this gym", Guid.Empty, HttpStatusCode.Forbidden);

        // 4) Check-in yarat
        var ci = new GymCheckIn
        {
            Id = Guid.NewGuid(),
            GymId = qr.GymId,
            UserId = UserId!,
            CheckInTime = now,
            Status = CheckInStatus.Active,
            Notes = dto.Notes
        };
        await _checkins.AddAsync(ci);

        // 5) One-time QR isə işarələ (concurrency token varsa EF ziddiyyəti atacaq)
        if (qr.IsOneTime)
        {
            qr.UsedAt = now;
            _qrRepo.Update(qr);
        }

        await _checkins.SaveChangeAsync(); // eyni scope-da olduqda bütün dəyişikliklər yazılır

        return new BaseResponse<Guid>("Checked in", ci.Id, HttpStatusCode.Created);
    }

    // ============== CHECK OUT ==============
    public async Task<BaseResponse<string>> CheckOutAsync(CheckOutRequestDto dto)
    {
        if (dto == null) return new("Request body is required", HttpStatusCode.BadRequest);
        if (string.IsNullOrWhiteSpace(UserId)) return new("Unauthorized", HttpStatusCode.Unauthorized);
        if (dto.CheckInId == Guid.Empty) return new("Check-in id is required", HttpStatusCode.BadRequest);

        // 1) MÜTLƏQ tracked entity götür
        var ci = await _checkins
            .GetAllFiltered(c => c.Id == dto.CheckInId && !c.IsDeleted, IsTracking: true)
            .FirstOrDefaultAsync();

        if (ci is null) return new("Check-in not found", HttpStatusCode.NotFound);
        if (ci.UserId != UserId) return new("Forbidden", HttpStatusCode.Forbidden);
        if (ci.Status != CheckInStatus.Active) return new("Already closed", HttpStatusCode.BadRequest);

        var now = DateTime.UtcNow;

        ci.CheckOutTime = now;
        ci.Status = CheckInStatus.CheckedOut;
        ci.UpdatedAt = now;

        // BU SƏTİR VACİBDİR: tracking qaranlıqdırsa belə, update-i zəmanətlə işlədir
        _checkins.Update(ci);

        try
        {
            await _checkins.SaveChangeAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return new("Concurrent update detected", HttpStatusCode.Conflict);
        }

        return new("Checked out", HttpStatusCode.OK);
    }

    // ============== MY ACTIVE ==============
    public async Task<BaseResponse<CheckInGetDto?>> GetMyActiveAsync()
    {
        if (string.IsNullOrWhiteSpace(UserId))
            return new BaseResponse<CheckInGetDto?>("Unauthorized", null, HttpStatusCode.Unauthorized);

        var query = _checkins
            .GetAllFiltered(
                x => x.UserId == UserId && x.Status == CheckInStatus.Active && !x.IsDeleted,
                include: new Expression<Func<GymCheckIn, object>>[] { x => x.Gym },
                IsTracking: false)
            .Include(x => x.Gym); // District lazım olsa: .ThenInclude(g => g.District)

        var c = await query
            .OrderByDescending(x => x.CheckInTime)
            .FirstOrDefaultAsync();

        if (c is null)
            return new BaseResponse<CheckInGetDto?>("No active check-in", null, HttpStatusCode.NotFound);

        return new BaseResponse<CheckInGetDto?>("Active check-in", new CheckInGetDto
        {
            Id = c.Id,
            GymId = c.GymId,
            GymName = c.Gym!.Name!,
            CheckInTime = c.CheckInTime,
            CheckOutTime = c.CheckOutTime,
            Status = c.Status.ToString(),
            Duration = c.Duration
        }, HttpStatusCode.OK);
    }

    // ============== MY HISTORY ==============
    public async Task<BaseResponse<List<CheckInGetDto>>> GetMyHistoryAsync(int days = 30)
    {
        if (string.IsNullOrWhiteSpace(UserId))
            return new BaseResponse<List<CheckInGetDto>>("Unauthorized", null, HttpStatusCode.Unauthorized);

        var since = DateTime.UtcNow.AddDays(-Math.Abs(days));

        var list = await _checkins
            .GetAllFiltered(
                x => x.UserId == UserId && !x.IsDeleted && x.CheckInTime >= since,
                include: new Expression<Func<GymCheckIn, object>>[] { x => x.Gym },
                IsTracking: false)
            .OrderByDescending(x => x.CheckInTime)
            .Select(x => new CheckInGetDto
            {
                Id = x.Id,
                GymId = x.GymId,
                GymName = x.Gym!.Name!,
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

    // ============== OCCUPANCY ==============
    public async Task<BaseResponse<int>> GetCurrentOccupancyAsync(Guid gymId)
    {
        var count = await _checkins
            .GetAllFiltered(c => c.GymId == gymId && c.Status == CheckInStatus.Active && !c.IsDeleted)
            .CountAsync();

        return new BaseResponse<int>("Occupancy retrieved", count, HttpStatusCode.OK);
    }
}

