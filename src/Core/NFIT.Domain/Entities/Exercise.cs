using NFIT.Domain.Enums;

namespace NFIT.Domain.Entities;

public class Exercise:BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public MuscleGroup PrimaryMuscleGroup { get; set; }
    public MuscleGroup[]? SecondaryMuscleGroups { get; set; }
    public EquipmentType Equipment { get; set; }
    public string? VideoUrl { get; set; }
    public DifficultyLevel Difficulty { get; set; }

    public ICollection<WorkoutExercise> WorkoutExercises { get; set; }
}
