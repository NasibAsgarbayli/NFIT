using NFIT.Application.DTOs.DistrictDtos;
using NFIT.Application.Shared;

namespace NFIT.Application.Abstracts.Services;

public interface IDistrictService
{
    Task<BaseResponse<Guid>> CreateAsync(DistrictCreateDto dto);
    Task<BaseResponse<string>> UpdateAsync(DistrictUpdateDto dto);
    Task<BaseResponse<string>> DeleteAsync(Guid id);                 // soft delete (+ IsActive=false)
    Task<BaseResponse<string>> DeactivateDistrictAsync(Guid id);     // IsActive=false
    Task<BaseResponse<int>> GetGymCountByDistrictAsync(Guid districtId, bool onlyActiveGyms = true);
    Task<BaseResponse<List<DistrictGetDto>>> GetAllAsync(); //only active
    Task<BaseResponse<string>> ActivateDistrictAsync(Guid id);

}
