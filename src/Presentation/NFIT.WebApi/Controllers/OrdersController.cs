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
      
        // ======================= GET =======================

        /// <summary>Hazırkı istifadəçinin bütün sifarişləri</summary>
        [Authorize] // sadə user yetərlidir
        [HttpGet("me")]
        [ProducesResponseType(typeof(BaseResponse<List<OrderGetDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<List<OrderGetDto>>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(BaseResponse<List<OrderGetDto>>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMyOrders()
        {
            var r = await _service.GetMyOrdersAsync();
            return StatusCode((int)r.StatusCode, r);
        }

        /// <summary>Satışlar (admin/moderator üçün)</summary>
        [Authorize(Policy = Permissions.Order.ViewSales)]
        [HttpGet("sales")]
        [ProducesResponseType(typeof(BaseResponse<List<OrderGetDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<List<OrderGetDto>>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(BaseResponse<List<OrderGetDto>>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(BaseResponse<List<OrderGetDto>>), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetSales([FromQuery] SalesFilterDto? filter)
        {
            var r = await _service.GetSalesAsync(filter);
            return StatusCode((int)r.StatusCode, r);
        }

        // ===================== CREATE ======================

        /// <summary>Supplement sifarişi yarat</summary>
        [Authorize]
        [HttpPost("supplements")]
        [ProducesResponseType(typeof(BaseResponse<Guid>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(BaseResponse<Guid>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(BaseResponse<Guid>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CreateSupp([FromBody] OrderCreateSupplementDto dto)
        {
            var r = await _service.CreateSupplementOrderAsync(dto);
            return StatusCode((int)r.StatusCode, r);
        }

        /// <summary>Subscription sifarişi yarat</summary>
        [Authorize]
        [HttpPost("subscriptions")]
        [ProducesResponseType(typeof(BaseResponse<Guid>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(BaseResponse<Guid>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(BaseResponse<Guid>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CreateSubs([FromBody] OrderCreateSubscriptionDto dto)
        {
            var r = await _service.CreateSubscriptionOrderAsync(dto);
            return StatusCode((int)r.StatusCode, r);
        }

        // ===================== CONFIRM =====================

        /// <summary>Sifarişi təsdiqlə (öz sifarişi və ya admin/moderator qaydaları)</summary>
        [Authorize(Policy = Permissions.Order.Confirm)]
        [HttpPost("{orderId:guid}/confirm")]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Confirm(Guid orderId)
        {
            var r = await _service.ConfirmOrderAsync(orderId);
            return StatusCode((int)r.StatusCode, r);
        }

        // ===================== UPDATE ======================

        /// <summary>Sifariş statusunu dəyiş (Admin/Moderator)</summary>
        [Authorize(Policy = Permissions.Order.UpdateStatus)]
        [HttpPatch("{orderId:guid}/status")]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateStatus(Guid orderId, [FromBody] OrderStatusUpdateDto dto)
        {
            var r = await _service.UpdateStatusAsync(orderId, dto);
            return StatusCode((int)r.StatusCode, r);
        }

        // ===================== DELETE ======================

        /// <summary>Sifarişi sil (Admin/Moderator)</summary>
        [Authorize(Policy = Permissions.Order.Delete)]
        [HttpDelete("{orderId:guid}")]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid orderId)
        {
            var r = await _service.DeleteAsync(orderId);
            return StatusCode((int)r.StatusCode, r);
        }
    }
}

