namespace NFIT.Application.DTOs.TrainerDtos;

public class TrainerWorkoutLineDto
{
    public Guid ExerciseId { get; set; }
    public int Sets { get; set; }
    public int Reps { get; set; }
    public int? Duration { get; set; }
    public int RestTimeSeconds { get; set; }
    public string? TrainerNotes { get; set; }
    public string? VideoUrl { get; set; }
}
