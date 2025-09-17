using System.Reflection;
using Microsoft.AspNetCore.Identity;
using NFIT.Domain.Enums;

namespace NFIT.Domain.Entities;

public class AppUser:IdentityUser
{
    public string FullName { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? ExpireDate { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public Gender Gender { get; set; }
    public decimal? Height { get; set; }
    public decimal? Weight { get; set; }
    public FitnessLevel FitnessLevel { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public string? Bio { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
    public PaymentMethod Payment { get; set; }
    public ICollection<Order> Orders { get; set; }
    public ICollection<Favourite> Favourites { get; set; } 
    public ICollection<Review> Reviews { get; set; } 
    public ICollection<GymCheckIn> GymCheckIns { get; set; } 
}
