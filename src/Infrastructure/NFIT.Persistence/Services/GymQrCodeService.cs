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

    // =========== CREATE OR ROTATE (UPDATE-PRIORITY) ===========
    public async Task<BaseResponse<GymQrGetDto>> GenerateOrRotateAsync(Guid gymId)
    {
        // 0) gym var?
        var gymExists = await _context.Gyms.AnyAsync(g => g.Id == gymId && !g.IsDeleted);
        if (!gymExists)
            return new BaseResponse<GymQrGetDto>("Gym not found", null, HttpStatusCode.NotFound);

        // 1) URL-safe 32-byte token
        var bytes = RandomNumberGenerator.GetBytes(32);
        var token = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(bytes);

        // 2) tək sətir modeli: varsa UPDATE, yoxdursa ADD
        var qr = await _context.GymQRCodes.FirstOrDefaultAsync(q => q.GymId == gymId);

        if (qr is null)
        {
            qr = new GymQRCode
            {
                GymId = gymId,
                QRCodeData = token,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            await _context.GymQRCodes.AddAsync(qr);
        }
        else
        {
            qr.QRCodeData = token;
            qr.IsActive = true;
            // One-time istifadə edirdinsə, rotasiyada təzələyirik:
            qr.UsedAt = null;
            // qr.ExpiresAt = DateTime.UtcNow.AddMinutes(5); // istəsən TTL
            qr.UpdatedAt = DateTime.UtcNow;

            _context.GymQRCodes.Update(qr);
        }

        await _context.SaveChangesAsync();

        return new BaseResponse<GymQrGetDto>("QR rotated", new GymQrGetDto
        {
            GymId = qr.GymId,
            QRCodeData = qr.QRCodeData,
            IsActive = qr.IsActive
        }, HttpStatusCode.Created);
    }

    // ================== DEACTIVATE ==================
    public async Task<BaseResponse<string>> DeactivateAsync(Guid gymId)
    {
        var qr = await _context.GymQRCodes
            .FirstOrDefaultAsync(q => q.GymId == gymId && !q.IsDeleted);

        if (qr is null || !qr.IsActive)
            return new BaseResponse<string>("No active QR", HttpStatusCode.NotFound);

        qr.IsActive = false;
        qr.UpdatedAt = DateTime.UtcNow;

        _context.GymQRCodes.Update(qr);
        await _context.SaveChangesAsync();

        return new BaseResponse<string>("QR deactivated", HttpStatusCode.OK);
    }
}
