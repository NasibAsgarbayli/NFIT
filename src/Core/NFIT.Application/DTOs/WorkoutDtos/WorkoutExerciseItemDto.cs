namespace NFIT.Application.DTOs.WorkoutDtos;

public class WorkoutExerciseItemDto
{
    public Guid ExerciseId { get; set; }
    public int Sets { get; set; }
    public int Reps { get; set; }
    public int? Duration { get; set; }
    public int RestTimeSeconds { get; set; }
}
