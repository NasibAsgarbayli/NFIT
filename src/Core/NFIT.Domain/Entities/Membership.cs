namespace NFIT.Domain.Entities;

public class Membership:BaseEntity
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }

    public AppUser User { get; set; } = null!;
    public Guid UserId { get; set; }

    public Gym GYM { get; set; } = null!;
    public Guid GymId { get; set; }

    public SubscriptionPlan SubscriptionPlan { get; set; } = null!;
    public Guid SubscriptionPlanId { get; set; }



}
