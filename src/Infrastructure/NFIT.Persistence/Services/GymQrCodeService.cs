using System.Net;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using NFIT.Application.Abstracts.Services;
using NFIT.Application.DTOs.GymQrCodeDtos;
using NFIT.Application.Shared;
using NFIT.Domain.Entities;
using NFIT.Persistence.Contexts;

namespace NFIT.Persistence.Services;

public class GymQrCodeService:IGymQrCodeService
{
    private readonly NFITDbContext _context;
    public GymQrCodeService(NFITDbContext context)
    {
        _context = context;
    }
    public async Task<BaseResponse<GymQrGetDto>> GetActiveAsync(Guid gymId)
    {
        var qr = await _context.GymQRCodes
            .AsNoTracking()
            .FirstOrDefaultAsync(q => q.GymId == gymId && q.IsActive && !q.IsDeleted);

        if (qr is null)
            return new BaseResponse<GymQrGetDto>("No active QR", null, HttpStatusCode.NotFound);

        return new BaseResponse<GymQrGetDto>("Active QR", new GymQrGetDto
        {
            GymId = qr.GymId,
            QRCodeData = qr.QRCodeData,
            IsActive = qr.IsActive
        }, HttpStatusCode.OK);
    }

    public async Task<BaseResponse<GymQrGetDto>> GenerateOrRotateAsync(Guid gymId)
    {
        var exists = await _context.Gyms.AnyAsync(g => g.Id == gymId && !g.IsDeleted);
        if (!exists) return new BaseResponse<GymQrGetDto>("Gym not found", null, HttpStatusCode.NotFound);

        // deactivate previous actives
        var olds = await _context.GymQRCodes
            .Where(q => q.GymId == gymId && q.IsActive && !q.IsDeleted)
            .ToListAsync();
        foreach (var q in olds) { q.IsActive = false; q.UpdatedAt = DateTime.UtcNow; }

        // secure random token
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        var qr = new GymQRCode { GymId = gymId, QRCodeData = token, IsActive = true };
        await _context.GymQRCodes.AddAsync(qr);
        await _context.SaveChangesAsync();

        return new BaseResponse<GymQrGetDto>("QR rotated", new GymQrGetDto
        {
            GymId = qr.GymId,
            QRCodeData = qr.QRCodeData,
            IsActive = qr.IsActive
        }, HttpStatusCode.Created);
    }

    public async Task<BaseResponse<string>> DeactivateAsync(Guid gymId)
    {
        var actives = await _context.GymQRCodes
            .Where(q => q.GymId == gymId && q.IsActive && !q.IsDeleted)
            .ToListAsync();

        if (actives.Count == 0)
            return new BaseResponse<string>("No active QR", HttpStatusCode.NotFound);

        foreach (var q in actives) { q.IsActive = false; q.UpdatedAt = DateTime.UtcNow; }
        await _context.SaveChangesAsync();
        return new BaseResponse<string>("QR deactivated", HttpStatusCode.OK);
    }
}
