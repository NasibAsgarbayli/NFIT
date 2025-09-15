using NFIT.Domain.Enums;

namespace NFIT.Application.DTOs.SubscriptionPlanDtos;

public class SubscriptionPlanGetDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public SubscriptionType Type { get; set; }
    public BillingCycle BillingCycle { get; set; }
    public decimal Price { get; set; }

    public int GymCount { get; set; }
}
