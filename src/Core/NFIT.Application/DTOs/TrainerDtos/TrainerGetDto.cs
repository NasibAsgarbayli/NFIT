namespace NFIT.Application.DTOs.TrainerDtos;

public class TrainerGetDto : TrainerListItemDto
{
    public string Bio { get; set; } = string.Empty;
    public string[] Specializations { get; set; } = Array.Empty<string>();
    public int ExperienceYears { get; set; }
    public string[] Certifications { get; set; } = Array.Empty<string>();
    public string? InstagramUrl { get; set; }
    public string? YoutubeUrl { get; set; }
}
