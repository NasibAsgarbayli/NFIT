namespace NFIT.Domain.Entities;

public class Favourite:BaseEntity
{
    public DateTime? AddedAt { get; set; }
    public string? UserId { get; set; }
    public AppUser? User { get; set; }


    public Guid? GymId { get; set; }
    public Gym? Gym { get; set; }

    public Guid? TrainerId { get; set; }
    public Trainer? Trainer { get; set; }

    public Guid? SupplementId { get; set; }
    public Supplement? Supplement { get; set; }
}
