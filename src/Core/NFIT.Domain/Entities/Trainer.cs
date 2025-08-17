namespace NFIT.Domain.Entities;

public class Trainer
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string[] Specializations { get; set; } = Array.Empty<string>();
    public int ExperienceYears { get; set; }
    public string[] Certifications { get; set; } = Array.Empty<string>();
    public string ProfilePictureUrl { get; set; } = string.Empty;
    public string? InstagramUrl { get; set; }
    public string? YoutubeUrl { get; set; }
    public decimal Rating { get; set; }
    public int TotalRatings { get; set; }
    public bool IsVerified { get; set; }
    public bool IsActive { get; set; }

    public ICollection<TrainerWorkout> TrainerWorkouts { get; set; }
    public ICollection<TrainerVideo> TrainerVideos { get; set; }
    public ICollection<Favourite> Favourites { get; set; }
    public ICollection<Review> Reviews { get; set; }

}
