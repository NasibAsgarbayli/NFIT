namespace NFIT.Domain.Entities;

public class WorkoutExercise:BaseEntity
{
    public Guid WorkoutId { get; set; }
    public Workout Workout { get; set; } = null!;
    public Guid ExerciseId { get; set; }
    public Exercise Exercise { get; set; } = null!;
    public int Sets { get; set; }
    public int Reps { get; set; }
    public int? Duration { get; set; }
    public int RestTimeSeconds { get; set; }
}
