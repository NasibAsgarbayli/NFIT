namespace NFIT.Application.DTOs.TrainerDtos;

public class TrainerCreateDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;

    public string[] Specializations { get; set; } = Array.Empty<string>();
    public int ExperienceYears { get; set; }
    public string[] Certifications { get; set; } = Array.Empty<string>();

    public string? InstagramUrl { get; set; }
    public string? YoutubeUrl { get; set; }

    // Owner kimsə əlavə edəcək (admin və ya trainer-ın özü)
    public string? UserId { get; set; }
}
