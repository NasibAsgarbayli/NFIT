namespace NFIT.Domain.Entities;

public class Category
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    public ICollection<Gym> Gyms { get; set; }
}
