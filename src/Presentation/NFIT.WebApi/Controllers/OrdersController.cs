using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NFIT.Application.Abstracts.Services;
using NFIT.Application.DTOs.OrderDtos;
using NFIT.Application.Shared;

namespace NFIT.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _service;
        public OrdersController(IOrderService service)
        {
            _service = service;
        }
        // ===== GET =====
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetMyOrders()
        {
            var r = await _service.GetMyOrdersAsync();
            return StatusCode((int)r.StatusCode, r);
        }

        //[Authorize(Policy = Permissions.Orders.ViewSales)]   // <-- sənin policy adlarına uyğunlaşdır
        [HttpGet("sales")]
        public async Task<IActionResult> GetSales([FromQuery] SalesFilterDto? filter)
        {
            var r = await _service.GetSalesAsync(filter);
            return StatusCode((int)r.StatusCode, r);
        }

        // ===== CREATE =====
        [Authorize]
        [HttpPost("supplements")]
        public async Task<IActionResult> CreateSupp([FromBody] OrderCreateSupplementDto dto)
        {
            var r = await _service.CreateSupplementOrderAsync(dto);
            return StatusCode((int)r.StatusCode, r);
        }

        [Authorize]
        [HttpPost("subscriptions")]
        public async Task<IActionResult> CreateSubs([FromBody] OrderCreateSubscriptionDto dto)
        {
            var r = await _service.CreateSubscriptionOrderAsync(dto);
            return StatusCode((int)r.StatusCode, r);
        }

        // ===== CONFIRM =====
        [Authorize]
        [HttpPost("{orderId:guid}/confirm")]
        public async Task<IActionResult> Confirm(Guid orderId)
        {
            var r = await _service.ConfirmOrderAsync(orderId); // <-- diqqət: method adı böyük hərflərlə ConfirmOrderAsync olmalıdır
            return StatusCode((int)r.StatusCode, r);
        }

        // ===== UPDATE =====
        //[Authorize(Policy = Permissions.Orders.Update)]
        [HttpPatch("{orderId:guid}/status")]
        public async Task<IActionResult> UpdateStatus(Guid orderId, [FromBody] OrderStatusUpdateDto dto)
        {
            var r = await _service.UpdateStatusAsync(orderId, dto);
            return StatusCode((int)r.StatusCode, r);
        }

        // ===== DELETE =====
        //[Authorize(Policy = Permissions.Orders.Delete)]
        [HttpDelete("{orderId:guid}")]
        public async Task<IActionResult> Delete(Guid orderId)
        {
            var r = await _service.DeleteAsync(orderId);
            return StatusCode((int)r.StatusCode, r);
        }
    }
}
