namespace NFIT.Domain.Entities;

public class Review:BaseEntity
{
    public bool IsApproved { get; set; } // Moderator təsdiqləməsi üçün
    public DateTime? ApprovedAt { get; set; }

    public Guid GYMID { get; set; }
    public Gym GYM { get; set; }

    public Guid TrainerId { get; set; }
    public Trainer Trainer { get; set; }

    public Guid SupplementId { get; set; }
    public Supplement Supplement { get; set; }

    public string UserId { get; set; }
    public AppUser User { get; set; }

    public string? Content { get; set; }
    public int Rating { get; set; }
    public DateTime CreatedAt { get; set; }
}
