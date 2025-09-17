using NFIT.Domain.Enums;

namespace NFIT.Application.DTOs.OrderDtos;

public class OrderCreateSubscriptionDto
{
    public Guid SubscriptionPlanId { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string? Note { get; set; }
}
