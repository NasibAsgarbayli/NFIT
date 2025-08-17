namespace NFIT.Domain.Entities;

public class Image
{
    public string ImageUrl { get; set; } = null!;

    public Guid GYMId { get; set; }
    public Gym GYM { get; set; }

    public Guid SupplementId { get; set; }
    public Supplement Supplement { get; set; }

}
