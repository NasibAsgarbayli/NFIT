using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NFIT.Application.Abstracts.Services;
using NFIT.Application.DTOs.GymChechkInDtos;
using NFIT.Application.Shared;

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
        [Authorize]
        [HttpGet("me/active")]
        [ProducesResponseType(typeof(BaseResponse<CheckInGetDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<CheckInGetDto>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(BaseResponse<CheckInGetDto>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> MyActive()
        {
            var result = await _service.GetMyActiveAsync();
            return StatusCode((int)result.StatusCode, result);
        }

        [Authorize]
        [HttpGet("me/history")]
        [ProducesResponseType(typeof(BaseResponse<List<CheckInGetDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<List<CheckInGetDto>>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(BaseResponse<List<CheckInGetDto>>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> MyHistory([FromQuery] int days = 30)
        {
            var result = await _service.GetMyHistoryAsync(days);
            return StatusCode((int)result.StatusCode, result);
        }

        [AllowAnonymous] // zalın sıxlığını hamı görə bilər
        [HttpGet("gyms/{gymId:guid}/occupancy(active users count)")]
        [ProducesResponseType(typeof(BaseResponse<int>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<int>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Occupancy(Guid gymId)
        {
            var result = await _service.GetCurrentOccupancyAsync(gymId);
            return StatusCode((int)result.StatusCode, result);
        }

        // ===== CREATE (Check-In) =====
        [Authorize]
        [HttpPost]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CheckIn([FromBody] CheckInRequestDto dto)
        {
            var result = await _service.CheckInAsync(dto);
            return StatusCode((int)result.StatusCode, result);
        }

        // ===== UPDATE (Check-Out) =====
        [Authorize]
        [HttpPost("checkout")]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CheckOut([FromBody] CheckOutRequestDto dto)
        {
            var result = await _service.CheckOutAsync(dto);
            return StatusCode((int)result.StatusCode, result);
        }
    }
}
