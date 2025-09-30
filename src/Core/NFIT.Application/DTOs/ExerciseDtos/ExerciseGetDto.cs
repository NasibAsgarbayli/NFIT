using NFIT.Domain.Enums;

namespace NFIT.Application.DTOs.ExerciseDtos;

public class ExerciseGetDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public MuscleGroup PrimaryMuscleGroup { get; set; }
    public MuscleGroup[]? SecondaryMuscleGroups { get; set; }
    public EquipmentType Equipment { get; set; }
    public string? VideoUrl { get; set; }
    public DifficultyLevel Difficulty { get; set; }
}
