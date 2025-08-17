namespace NFIT.Domain.Entities;

public class GymCategory:BaseEntity
{
    public Guid GYMId { get; set; }
    public Gym GYM { get; set; } = null!;

    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
}
