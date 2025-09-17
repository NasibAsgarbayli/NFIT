namespace NFIT.Domain.Entities;

public class TrainerWorkoutExercise : BaseEntity
{
    public Guid TrainerWorkoutId { get; set; }
    public TrainerWorkout TrainerWorkout { get; set; } = null!;
    public Guid ExerciseId { get; set; }
    public Exercise Exercise { get; set; } = null!;
    public int Sets { get; set; }
    public int Reps { get; set; }
    public int? Duration { get; set; }
    public int RestTimeSeconds { get; set; }
    public string? TrainerNotes { get; set; }
    public string? VideoUrl { get; set; }
}
