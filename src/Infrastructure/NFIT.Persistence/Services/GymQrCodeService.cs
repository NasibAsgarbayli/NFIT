using System.Net;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using NFIT.Application.Abstracts.Repositories;
using NFIT.Application.Abstracts.Services;
using NFIT.Application.DTOs.GymQrCodeDtos;
using NFIT.Application.Shared;
using NFIT.Domain.Entities;
using NFIT.Persistence.Contexts;

namespace NFIT.Persistence.Services;

public class GymQrCodeService:IGymQrCodeService
{
    private readonly IGymRepository _gymRepo;
    private readonly IGymQrCodeRepository _qrRepo;

    public GymQrCodeService(IGymRepository gymRepo, IGymQrCodeRepository qrRepo)
    {
        _gymRepo = gymRepo;
        _qrRepo = qrRepo;
    }

    public async Task<BaseResponse<GymQrGetDto>> GetActiveAsync(Guid gymId)
    {
        var qr = await _qrRepo
            .GetAllFiltered(q => q.GymId == gymId && q.IsActive && !q.IsDeleted, IsTracking: false)
            .FirstOrDefaultAsync();

        if (qr is null)
            return new BaseResponse<GymQrGetDto>("No active QR", null, HttpStatusCode.NotFound);

        return new BaseResponse<GymQrGetDto>("Active QR", new GymQrGetDto
        {
            GymId = qr.GymId,
            QRCodeData = qr.QRCodeData,
            IsActive = qr.IsActive
        }, HttpStatusCode.OK);
    }

    // CREATE or ROTATE
    public async Task<BaseResponse<GymQrGetDto>> GenerateOrRotateAsync(Guid gymId)
    {
        // 0) gym var?
        var gymExists = await _gymRepo
            .GetAllFiltered(g => g.Id == gymId && !g.IsDeleted, IsTracking: false)
            .AnyAsync();

        if (!gymExists)
            return new BaseResponse<GymQrGetDto>("Gym not found", null, HttpStatusCode.NotFound);

        // 1) URL-safe 32-byte token
        var bytes = RandomNumberGenerator.GetBytes(32);
        var token = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(bytes);

        // 2) varsa UPDATE, yoxdursa ADD
        var qr = await _qrRepo
            .GetAllFiltered(q => q.GymId == gymId && !q.IsDeleted, IsTracking: true)
            .FirstOrDefaultAsync();

        if (qr is null)
        {
            qr = new GymQRCode
            {
                GymId = gymId,
                QRCodeData = token,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            await _qrRepo.AddAsync(qr);
        }
        else
        {
            qr.QRCodeData = token;
            qr.IsActive = true;
            qr.UsedAt = null;                 // one-time istifadə yenilənir
            // qr.ExpiresAt = DateTime.UtcNow.AddMinutes(5); // istəsən TTL
            qr.UpdatedAt = DateTime.UtcNow;

            _qrRepo.Update(qr);
        }

        await _qrRepo.SaveChangeAsync();

        return new BaseResponse<GymQrGetDto>("QR rotated", new GymQrGetDto
        {
            GymId = qr.GymId,
            QRCodeData = qr.QRCodeData,
            IsActive = qr.IsActive
        }, HttpStatusCode.Created);
    }

    public async Task<BaseResponse<string>> DeactivateAsync(Guid gymId)
    {
        var qr = await _qrRepo
            .GetAllFiltered(q => q.GymId == gymId && !q.IsDeleted, IsTracking: true)
            .FirstOrDefaultAsync();

        if (qr is null || !qr.IsActive)
            return new BaseResponse<string>("No active QR", HttpStatusCode.NotFound);

        qr.IsActive = false;
        qr.UpdatedAt = DateTime.UtcNow;

        _qrRepo.Update(qr);
        await _qrRepo.SaveChangeAsync();

        return new BaseResponse<string>("QR deactivated", HttpStatusCode.OK);
    }
}
