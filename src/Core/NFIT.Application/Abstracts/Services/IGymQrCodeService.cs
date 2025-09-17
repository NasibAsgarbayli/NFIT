using NFIT.Application.DTOs.GymQrCodeDtos;
using NFIT.Application.Shared;

namespace NFIT.Application.Abstracts.Services;

public interface IGymQrCodeService
{
    Task<BaseResponse<GymQrGetDto>> GetActiveAsync(Guid gymId);
    Task<BaseResponse<GymQrGetDto>> GenerateOrRotateAsync(Guid gymId); // köhnəni deaktiv edib yenisini yaradır
    Task<BaseResponse<string>> DeactivateAsync(Guid gymId);
}
