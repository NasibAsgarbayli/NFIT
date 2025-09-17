using NFIT.Application.DTOs.SubscriptionPlanDtos;
using NFIT.Application.Shared;

namespace NFIT.Application.Abstracts.Services;

public interface ISubscriptionPlanService
{
    Task<BaseResponse<Guid>> CreateAsync(SubscriptionPlanCreateDto dto);
    Task<BaseResponse<string>> UpdateAsync(SubscriptionPlanUpdateDto dto);
    Task<BaseResponse<string>> DeleteAsync(Guid id); // soft delete
    Task<BaseResponse<List<SubscriptionPlanGetDto>>> GetAllAsync();
    Task<BaseResponse<SubscriptionPlanGetDto>> GetByIdAsync(Guid id);
    Task<BaseResponse<SubscriptionPlanGetDto>> GetByNameAsync(string name);
    Task<BaseResponse<SubscriptionPlanGetDto>> GetCheapestPlanAsync();                // ən ucuz plan
    Task<BaseResponse<SubscriptionPlanGetDto>> GetMostExpensivePlanAsync();           // ən bahalı plan
    Task<BaseResponse<List<SubscriptionPlanGetDto>>> GetPlansByGymCountAsync(bool descending = true); // gym sayına görə sırala
}
