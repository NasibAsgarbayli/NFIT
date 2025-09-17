using NFIT.Domain.Enums;

namespace NFIT.Domain.Entities;

public class SubscriptionPlan : BaseEntity
{
    public string Name { get; set; }
    public string Description { get; set; }
    public SubscriptionType Type { get; set; }
    public BillingCycle BillingCycle { get; set; }
    public decimal Price { get; set; }

    public ICollection<Gym> Gyms { get; set; }
}
