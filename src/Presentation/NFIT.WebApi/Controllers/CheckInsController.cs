using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NFIT.Application.Abstracts.Services;
using NFIT.Application.DTOs.GymChechkInDtos;

namespace NFIT.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CheckInsController : ControllerBase
    {
        private readonly IGymCheckInService _service;
        public CheckInsController(IGymCheckInService service)
        {
            _service = service;
        }
        // ===== GET =====
        [HttpGet("me/active")]
        public async Task<IActionResult> MyActive()
            => StatusCode((int)(await _service.GetMyActiveAsync()).StatusCode,
                          await _service.GetMyActiveAsync());

        [HttpGet("me/history")]
        public async Task<IActionResult> MyHistory([FromQuery] int days = 30)
            => StatusCode((int)(await _service.GetMyHistoryAsync(days)).StatusCode,
                          await _service.GetMyHistoryAsync(days));

        [HttpGet("gyms/{gymId:guid}/occupancy")]
        public async Task<IActionResult> Occupancy(Guid gymId)
            => StatusCode((int)(await _service.GetCurrentOccupancyAsync(gymId)).StatusCode,
                          await _service.GetCurrentOccupancyAsync(gymId));

        // ===== CREATE (Check-In) =====
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CheckIn([FromBody] CheckInRequestDto dto)
            => StatusCode((int)(await _service.CheckInAsync(dto)).StatusCode,
                          await _service.CheckInAsync(dto));

        // ===== UPDATE (Check-Out) =====
        [Authorize]
        [HttpPost("checkout")]
        public async Task<IActionResult> CheckOut([FromBody] CheckOutRequestDto dto)
            => StatusCode((int)(await _service.CheckOutAsync(dto)).StatusCode,
                          await _service.CheckOutAsync(dto));
    }
}
