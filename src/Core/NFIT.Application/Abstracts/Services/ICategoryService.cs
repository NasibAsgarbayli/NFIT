using NFIT.Application.DTOs.CategoryDtos;
using NFIT.Application.Shared;

namespace NFIT.Application.Abstracts.Services;

public interface ICategoryService
{
    Task<BaseResponse<Guid>> CreateAsync(CategoryCreateDto dto);
    Task<BaseResponse<string>> UpdateAsync(CategoryUpdateDto dto);
    Task<BaseResponse<string>> DeleteAsync(Guid id); // soft delete
    Task<BaseResponse<CategoryGetDto>> GetByIdAsync(Guid id);
    Task<BaseResponse<List<CategoryGetDto>>> GetAllAsync();
    Task<BaseResponse<CategoryGetDto>> GetByNameAsync(string name);                 // Ada görə tap (unikal ad sayırıq)
    Task<BaseResponse<CategoryWithGymsDto>> GetByIdWithGymsAsync(Guid id);         // ID ilə, gym-larla birlikdə
    Task<BaseResponse<int>> GetCategoryCountAsync();
}
