namespace NFIT.Application.DTOs.GymDtos;

public class GymAddSubscriptionsRequest
{
    public List<Guid> SubscriptionPlanIds { get; set; } = new();
}
