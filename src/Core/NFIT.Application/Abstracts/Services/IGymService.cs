using Microsoft.AspNetCore.Http;
using NFIT.Application.DTOs.GymDtos;
using NFIT.Application.Shared;

namespace NFIT.Application.Abstracts.Services;

public interface IGymService
{
    Task<BaseResponse<Guid>> CreateAsync(GymCreateDto dto);
    Task<BaseResponse<string>> UpdateAsync(GymUpdateDto dto);
    Task<BaseResponse<string>> DeleteAsync(Guid id);     
    Task<BaseResponse<GymDetailsDto>> GetByIdAsync(Guid id);
    Task<BaseResponse<List<GymListItemDto>>> GetAllAsync(int page = 1, int pageSize = 20);

    Task<BaseResponse<string>> AddImageAsync(Guid gymId, IFormFile image);
    Task<BaseResponse<string>> DeleteImageAsync(Guid gymId, Guid imageId);
    Task<BaseResponse<string>> AddCategoriesOnlyAsync(Guid gymId, List<Guid> categoryIds);
    Task<BaseResponse<string>> AddSubscriptionsOnlyAsync(Guid gymId, List<Guid> subscriptionIds);
}
