using NFIT.Domain.Enums;

namespace NFIT.Application.DTOs.TrainerDtos;

public class TrainerVideoCreateDto
{
    public Guid TrainerId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string VideoUrl { get; set; } = string.Empty; // upload-dan sonra da set edilə bilər
    public string? ThumbnailUrl { get; set; }
    public int Duration { get; set; }
    public VideoType Type { get; set; }
    public WorkoutCategory? Category { get; set; }
    public bool IsPremium { get; set; }
    public DateTime? PublishedAt { get; set; }
}
