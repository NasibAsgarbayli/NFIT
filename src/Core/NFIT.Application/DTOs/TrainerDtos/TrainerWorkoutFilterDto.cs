using NFIT.Domain.Enums;

namespace NFIT.Application.DTOs.TrainerDtos;

public class TrainerWorkoutFilterDto
{
    public Guid? TrainerId { get; set; }
    public WorkoutCategory? Category { get; set; }
    public DifficultyLevel? Difficulty { get; set; }
    public bool? IsPremium { get; set; }

    public string? SortBy { get; set; } = "published"; // published|views|likes
    public bool Desc { get; set; } = true;

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
