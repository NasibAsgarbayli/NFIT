using NFIT.Domain.Enums;

namespace NFIT.Application.DTOs.TrainerDtos;

public class TrainerWorkoutCreateDto
{
    public Guid TrainerId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public WorkoutCategory Category { get; set; }
    public DifficultyLevel Difficulty { get; set; }
    public int EstimatedDuration { get; set; }
    public MuscleGroup TargetMuscles { get; set; }
    public EquipmentType RequiredEquipment { get; set; }

    public string? ThumbnailUrl { get; set; }
    public string? PreviewVideoUrl { get; set; }
    public bool IsPremium { get; set; }
    public DateTime? PublishedAt { get; set; }

    public List<TrainerWorkoutLineDto> Lines { get; set; } = new();
}
