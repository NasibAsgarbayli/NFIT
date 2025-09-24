namespace NFIT.Application.DTOs.TrainerDtos;

public class TrainerVideoListItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public int Duration { get; set; }
    public bool IsPremium { get; set; }
    public int ViewCount { get; set; }
    public int LikeCount { get; set; }
    public DateTime PublishedAt { get; set; }
}
