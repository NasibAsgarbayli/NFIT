using NFIT.Domain.Enums;

namespace NFIT.Application.DTOs.OrderDtos;

public sealed class OrderStatusUpdateDto
{
    public SupplementOrderStatus Status { get; set; }
}
