namespace NFIT.Domain.Entities;

public class GymCategory:BaseEntity
{
    public Guid GymId { get; set; }
    public Gym Gym { get; set; } = null!;

    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
}
