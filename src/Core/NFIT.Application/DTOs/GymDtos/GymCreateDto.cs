namespace NFIT.Application.DTOs.GymDtos;

public class GymCreateDto
{
    public string Name { get; set; } 
    public string? Description { get; set; }
    public string Address { get; set; } 
    public Guid DistrictId { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? InstagramLink { get; set; }
    public bool IsPremium { get; set; } 
    public bool IsActive { get; set; }

    public List<Guid> CategoryIds { get; set; } 
    public List<Guid> SubscriptionPlanIds { get; set; }
}
