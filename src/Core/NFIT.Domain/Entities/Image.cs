namespace NFIT.Domain.Entities;

public class Image:BaseEntity
{
    public string ImageUrl { get; set; } = null!;
    public string PublicId { get; set; } = default!;
    public Guid? GymId { get; set; }
    public Gym? Gym { get; set; }

    public Guid? SupplementId { get; set; }
    public Supplement? Supplement { get; set; }

    public Guid? TrainerId { get; set; }
    public Trainer? Trainer { get;set; }

}
