using NFIT.Application.DTOs.SupplementDtos;
using NFIT.Application.Shared;

namespace NFIT.Application.Abstracts.Services;

public interface ISupplementService
{
    Task<BaseResponse<Guid>> CreateAsync(SupplementCreateDto dto);
    Task<BaseResponse<string>> UpdateAsync(SupplementUpdateDto dto);
    Task<BaseResponse<string>> DeleteAsync(Guid id); 

    Task<BaseResponse<SupplementGetDto>> GetByIdAsync(Guid id);
    Task<BaseResponse<List<SupplementGetDto>>> GetAllAsync(SupplementFilterDto? filter = null);

    Task<BaseResponse<List<SupplementGetDto>>> SearchByNameAsync(string name);
    Task<BaseResponse<List<SupplementGetDto>>> GetByBrandAsync(string brand);

    // Favorit sayına görə ən populyar (top N)
    Task<BaseResponse<List<SupplementGetDto>>> GetPopularSupplementsAsync(int top = 10);
}
