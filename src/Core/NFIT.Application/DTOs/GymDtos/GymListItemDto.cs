namespace NFIT.Application.DTOs.GymDtos;

public class GymListItemDto
{

    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Address { get; set; } = default!;
    public string DistrictName { get; set; } = default!;
    public bool IsPremium { get; set; }
    public bool IsActive { get; set; }
    public decimal Rating { get; set; }
    public int CategoryCount { get; set; }
    public int SubscriptionCount { get; set; }
    public List<string> CategoryNames { get; set; } = new();
    public List<string> SubscriptionNames { get; set; } = new();



}
