using NFIT.Domain.Enums;

namespace NFIT.Application.DTOs.SubscriptionPlanDtos;

public class SubscriptionPlanCreateDto
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public SubscriptionType Type { get; set; }
    public BillingCycle BillingCycle { get; set; }
    public decimal Price { get; set; }
}
