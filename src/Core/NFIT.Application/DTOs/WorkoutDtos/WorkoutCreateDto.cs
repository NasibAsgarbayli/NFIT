using NFIT.Domain.Enums;

namespace NFIT.Application.DTOs.WorkoutDtos;

public class WorkoutCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int EstimatedDuration { get; set; }
    public DifficultyLevel Difficulty { get; set; }
    public WorkoutCategory Category { get; set; }
    public MuscleGroup[]? TargetMuscles { get; set; }
    public string[]? RequiredEquipment { get; set; }
    public string? VideoUrl { get; set; }
    public bool IsPublic { get; set; } = true;
    public List<WorkoutExerciseItemDto> Exercises { get; set; } = new();
}
