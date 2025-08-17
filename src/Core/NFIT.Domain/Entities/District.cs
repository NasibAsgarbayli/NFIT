namespace NFIT.Domain.Entities;

public class District:BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // Computed property - həmişə Baku qaytarır
    public string City => "Baku";

    public ICollection<Gym> Gyms { get; set; }
}
