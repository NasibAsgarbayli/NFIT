namespace NFIT.Domain.Entities;

public class Gym:BaseEntity
{
    public string? Name { get; set; } 
    public string? Description { get; set; } 
    public string? Address { get; set; }
    public Guid DistrictId { get; set; }
    public District District { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? InstagramLink { get; set; }
    public bool IsPremium { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public decimal Rating { get; set; }

    public ICollection<GymCategory> GymCategories { get; set; }
    public ICollection<Favourite> Favourites { get; set; }
    public ICollection<SubscriptionPlan> AvailableSubscriptions { get; set; }
    public ICollection<Review> Reviews { get; set; }
    public ICollection<Image> Images { get; set; }
    public ICollection<GymCheckIn> CheckIns { get; set; }
    public GymQRCode? QRCode { get; set; }
}
