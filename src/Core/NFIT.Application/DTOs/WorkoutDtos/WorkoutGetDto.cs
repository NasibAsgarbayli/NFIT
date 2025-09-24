using NFIT.Domain.Enums;

namespace NFIT.Application.DTOs.WorkoutDtos;

public class WorkoutGetDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int EstimatedDuration { get; set; }
    public DifficultyLevel Difficulty { get; set; }
    public WorkoutCategory Category { get; set; }
    public MuscleGroup[]? TargetMuscles { get; set; }
    public string[]? RequiredEquipment { get; set; }
    public string? VideoUrl { get; set; }
    public bool IsPublic { get; set; }
    public bool IsActive { get; set; }
    public List<WorkoutExerciseDetailDto> Exercises { get; set; } = new();
}
