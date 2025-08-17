using NFIT.Domain.Enums;

namespace NFIT.Domain.Entities;

public class Workout:BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int EstimatedDuration { get; set; }
    public DifficultyLevel Difficulty { get; set; }
    public WorkoutCategory Category { get; set; }
    public string[]? TargetMuscles { get; set; }
    public string[]? RequiredEquipment { get; set; }
    public string? VideoUrl { get; set; }
    public bool IsPublic { get; set; } = true;
    public ICollection<WorkoutExercise> WorkoutExercises { get; set; }

}
