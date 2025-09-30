namespace NFIT.Application.DTOs.WorkoutDtos;

public class WorkoutExerciseDetailDto
{
    public Guid ExerciseId { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public int Sets { get; set; }
    public int Reps { get; set; }
    public int? Duration { get; set; }
    public int RestTimeSeconds { get; set; }
}
