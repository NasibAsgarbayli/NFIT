namespace NFIT.Application.DTOs.GymDtos;

public class GymDetailsDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string Address { get; set; } = default!;
    public Guid DistrictId { get; set; }
    public string DistrictName { get; set; } = default!;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? InstagramLink { get; set; }
    public bool IsPremium { get; set; }
    public bool IsActive { get; set; }
    public decimal Rating { get; set; }

    public List<string> Categories { get; set; } = new();
    public List<string> Subscriptions { get; set; } = new();

}
