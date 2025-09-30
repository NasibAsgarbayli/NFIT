using NFIT.Domain.Enums;

namespace NFIT.Application.DTOs.OrderDtos;

public sealed class OrderGetDto
{
    public Guid Id { get; set; }
    public DateTime OrderDate { get; set; }
    public SupplementOrderStatus Status { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public decimal? TotalPrice { get; set; }
    public string? Note { get; set; }
    public string? DeliveryAddress { get; set; }

    // Əgər subscription alınıbsa:
    public Guid? SubscriptionPlanId { get; set; }
    public string? SubscriptionPlanName { get; set; }

    // Supplement line-ları
    public List<OrderSupplementLine> Lines { get; set; } = new();
    public sealed class OrderSupplementLine
    {
        public Guid SupplementId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }       // snapshot
        public decimal LineTotal => UnitPrice * Quantity;
    }
}
