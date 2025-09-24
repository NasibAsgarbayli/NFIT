using NFIT.Domain.Enums;

namespace NFIT.Application.DTOs.TrainerDtos;

public class TrainerVideoFeedFilterDto
{
    public string? Search { get; set; }
    public WorkoutCategory? Category { get; set; }
    public VideoType? Type { get; set; }
    public bool? IsPremium { get; set; }

    public string? SortBy { get; set; } = "published"; // published|views|likes
    public bool Desc { get; set; } = true;

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
