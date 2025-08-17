using NFIT.Domain.Enums;

namespace NFIT.Domain.Entities;

public class TrainerWorkout : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid TrainerId { get; set; }
    public Trainer Trainer { get; set; } = null!;
    public WorkoutCategory Category { get; set; }
    public DifficultyLevel Difficulty { get; set; }
    public int EstimatedDuration { get; set; }
    public MuscleGroup TargetMuscles { get; set; }
    public EquipmentType RequiredEquipment { get; set; }
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string? PreviewVideoUrl { get; set; }
    public bool IsPremium { get; set; }
    public int ViewCount { get; set; }
    public int LikeCount { get; set; }
    public DateTime PublishedAt { get; set; }

    public ICollection<TrainerWorkoutExercise> WorkoutExercises { get; set; }
  
}
