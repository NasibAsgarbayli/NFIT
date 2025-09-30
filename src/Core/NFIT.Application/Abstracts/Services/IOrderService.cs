using NFIT.Application.DTOs.OrderDtos;
using NFIT.Application.Shared;

namespace NFIT.Application.Abstracts.Services;

public interface IOrderService
{
    // CREATE
    Task<BaseResponse<Guid>> CreateSupplementOrderAsync(OrderCreateSupplementDto dto);
    Task<BaseResponse<Guid>> CreateSubscriptionOrderAsync(OrderCreateSubscriptionDto dto);

    // CONFIRM (ödəniş tamamlandı → subscription olduqda membership yaradılır)
    Task<BaseResponse<string>> ConfirmOrderAsync(Guid orderId);

    // READ
    Task<BaseResponse<List<OrderGetDto>>> GetMyOrdersAsync();
    Task<BaseResponse<List<OrderGetDto>>> GetSalesAsync(SalesFilterDto? filter = null); // Admin/Moderator

    // UPDATE
    Task<BaseResponse<string>> UpdateStatusAsync(Guid orderId, OrderStatusUpdateDto dto);

    // DELETE (soft)
    Task<BaseResponse<string>> DeleteAsync(Guid orderId);
}
