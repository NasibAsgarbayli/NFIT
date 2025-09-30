using Microsoft.AspNetCore.Http;
using NFIT.Application.DTOs.TrainerDtos;
using NFIT.Application.Shared;
using NFIT.Domain.Enums;

namespace NFIT.Application.Abstracts.Services;

public interface ITrainerService
{
    // Trainer (profile)
    Task<BaseResponse<Guid>> CreateAsync(TrainerCreateDto dto);
    Task<BaseResponse<string>> UpdateAsync(TrainerUpdateDto dto);
    Task<BaseResponse<string>> DeleteAsync(Guid id);
    Task<BaseResponse<string>> AddImageAsync(Guid trainerId, TrainerImageUploadPhotoDto dto);
    Task<BaseResponse<string>> DeleteImageAsync(Guid trainerId, Guid imageId);
    Task<BaseResponse<string>> VerifyAsync(Guid id);
    Task<BaseResponse<string>> ToggleActiveAsync(Guid id, bool isActive);

    Task<BaseResponse<TrainerGetDto>> GetByIdAsync(Guid id);
    Task<BaseResponse<List<TrainerListItemDto>>> GetAllAsync(TrainerFilterDto filter);
    Task<BaseResponse<List<TrainerListItemDto>>> GetTopAsync(int top = 10);
    Task<BaseResponse<List<TrainerListItemDto>>> GetByNameAsync(string name);

    // Videos
    Task<BaseResponse<Guid>> CreateVideoAsync(TrainerVideoCreateDto dto);
    Task<BaseResponse<string>> UpdateVideoAsync(TrainerVideoUpdateDto dto);
    Task<BaseResponse<string>> DeleteVideoAsync(Guid videoId);
    Task<BaseResponse<TrainerVideoGetDto>> GetVideoByIdAsync(Guid videoId);
    Task<BaseResponse<List<TrainerVideoListItemDto>>> GetTrainerVideosAsync(Guid trainerId, int page = 1, int pageSize = 20);
    Task<BaseResponse<List<TrainerVideoListItemDto>>> GetVideoFeedAsync(TrainerVideoFeedFilterDto filter);
    Task<BaseResponse<List<TrainerVideoListItemDto>>> GetVideosByNameAsync(string name);
    Task<BaseResponse<List<TrainerVideoListItemDto>>> GetRecentVideosAsync(int days = 30, int page = 1, int pageSize = 20);
    Task<BaseResponse<List<TrainerVideoListItemDto>>> GetPopularVideosAsync(int page = 1, int pageSize = 20);


    // Workouts
    Task<BaseResponse<Guid>> CreateWorkoutAsync(TrainerWorkoutCreateDto dto);
    Task<BaseResponse<string>> UpdateWorkoutAsync(TrainerWorkoutUpdateDto dto);
    Task<BaseResponse<string>> DeleteWorkoutAsync(Guid workoutId);
    Task<BaseResponse<TrainerWorkoutGetDto>> GetWorkoutByIdAsync(Guid workoutId);
    Task<BaseResponse<List<TrainerWorkoutListItemDto>>> GetTrainerWorkoutsAsync(Guid trainerId, int page = 1, int pageSize = 20);
    Task<BaseResponse<List<TrainerWorkoutListItemDto>>> GetWorkoutFeedAsync(TrainerWorkoutFilterDto filter);
    Task<BaseResponse<List<TrainerWorkoutListItemDto>>> GetWorkoutsByNameAsync(string name);
    Task<BaseResponse<List<TrainerWorkoutListItemDto>>> GetWorkoutsByDifficultyAsync(DifficultyLevel level, int page = 1, int pageSize = 20);
    Task<BaseResponse<List<TrainerWorkoutListItemDto>>> GetWorkoutsByCategoryAsync(WorkoutCategory category, int page = 1, int pageSize = 20);
  

}
