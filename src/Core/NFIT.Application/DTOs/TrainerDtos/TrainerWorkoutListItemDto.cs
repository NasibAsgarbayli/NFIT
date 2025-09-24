using NFIT.Domain.Enums;

namespace NFIT.Application.DTOs.TrainerDtos;

public class TrainerWorkoutListItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public bool IsPremium { get; set; }
    public int ViewCount { get; set; }
    public int LikeCount { get; set; }
    public DateTime PublishedAt { get; set; }
    public DifficultyLevel Difficulty { get; set; }
    public WorkoutCategory Category { get; set; }
}
