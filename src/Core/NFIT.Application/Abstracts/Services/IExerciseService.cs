using NFIT.Application.DTOs.ExerciseDtos;
using NFIT.Application.Shared;
using NFIT.Domain.Enums;

namespace NFIT.Application.Abstracts.Services;

public interface IExerciseService
{
    Task<BaseResponse<Guid>> CreateAsync(ExerciseCreateDto dto);
    Task<BaseResponse<string>> UpdateAsync(ExerciseUpdateDto dto);
    Task<BaseResponse<string>> DeleteAsync(Guid id); // soft delete

    Task<BaseResponse<ExerciseGetDto>> GetByNameAsync(string name);
    Task<BaseResponse<List<ExerciseGetDto>>> GetByMuscleGroupAsync(MuscleGroup muscle);
    Task<BaseResponse<ExerciseGetDto>> GetByIdAsync(Guid id);
    Task<BaseResponse<List<ExerciseGetDto>>> GetAllAsync();
}
