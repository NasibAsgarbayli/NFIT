using NFIT.Application.DTOs.GymChechkInDtos;
using NFIT.Application.Shared;

namespace NFIT.Application.Abstracts.Services;

public interface IGymCheckInService
{
    Task<BaseResponse<Guid>> CheckInAsync(CheckInRequestDto dto);
    Task<BaseResponse<string>> CheckOutAsync(CheckOutRequestDto dto);
    Task<BaseResponse<CheckInGetDto?>> GetMyActiveAsync();
    Task<BaseResponse<List<CheckInGetDto>>> GetMyHistoryAsync(int days = 30);
    Task<BaseResponse<int>> GetCurrentOccupancyAsync(Guid gymId);
}
