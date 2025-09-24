using NFIT.Domain.Enums;

namespace NFIT.Application.DTOs.TrainerDtos;

public class TrainerVideoGetDto : TrainerVideoListItemDto
{
    public string Description { get; set; } = string.Empty;
    public string VideoUrl { get; set; } = string.Empty;
    public VideoType Type { get; set; }
    public WorkoutCategory? Category { get; set; }
}
