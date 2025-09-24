using NFIT.Application.DTOs.WorkoutDtos;
using NFIT.Application.Shared;
using NFIT.Domain.Enums;

namespace NFIT.Application.Abstracts.Services;

public interface IWorkoutService
{
    Task<BaseResponse<Guid>> CreateAsync(WorkoutCreateDto dto);
    Task<BaseResponse<string>> UpdateAsync(WorkoutUpdateDto dto);
    Task<BaseResponse<string>> DeleteAsync(Guid id);

    Task<BaseResponse<WorkoutGetDto>> GetByIdAsync(Guid id);
    Task<BaseResponse<List<WorkoutGetDto>>> GetAllAsync();
    Task<BaseResponse<List<WorkoutGetDto>>> GetByCategoryAsync(WorkoutCategory category);
}
