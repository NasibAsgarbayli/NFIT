using NFIT.Domain.Enums;

namespace NFIT.Application.DTOs.OrderDtos;

public sealed class OrderCreateSupplementDto
{
    public PaymentMethod PaymentMethod { get; set; }
    public string? Note { get; set; }
    public string? DeliveryAddress { get; set; }

    // səbət maddələri (id + qty)
    public List<OrderItemDto> Items { get; set; } = new();
    public sealed class OrderItemDto
    {
        public Guid SupplementId { get; set; }
        public int Quantity { get; set; }
    }
}
