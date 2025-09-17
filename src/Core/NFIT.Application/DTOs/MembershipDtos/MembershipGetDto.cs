namespace NFIT.Application.DTOs.MembershipDtos;

public sealed class MembershipGetDto
{
    public Guid Id { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public string PlanType { get; set; } = string.Empty;       // SubscriptionType as string
    public string BillingCycle { get; set; } = string.Empty;   // BillingCycle as string
    public decimal Price { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
}
