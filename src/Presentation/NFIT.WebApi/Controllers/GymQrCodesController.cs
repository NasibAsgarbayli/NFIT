using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NFIT.Application.Abstracts.Services;
using NFIT.Application.Shared;

namespace NFIT.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GymQrCodesController : ControllerBase
    {
        private readonly IGymQrCodeService _service;
        public GymQrCodesController(IGymQrCodeService service)
        {
            _service = service;
        }

        // ======= GET =======

        /// <summary>Gym üçün aktiv QR kodu gətir</summary>
        [HttpGet("active")]
        [Authorize]
        [ProducesResponseType(typeof(BaseResponse<string>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(BaseResponse<string>), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetActive([FromQuery] Guid gymId)
        {
            var r = await _service.GetActiveAsync(gymId);
            return StatusCode((int)r.StatusCode, r);
        }

        /// <summary>Gym üçün yeni QR yarat və ya mövcudunu rotasiya et</summary>
        [HttpPost("rotate")]
        [Authorize]
        [ProducesResponseType(typeof(BaseResponse<string>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(BaseResponse<string>), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Rotate([FromQuery] Guid gymId)
        {
            var r = await _service.GenerateOrRotateAsync(gymId);
            return StatusCode((int)r.StatusCode, r);
        }

        /// <summary>Gym üçün aktiv QR kodu deaktiv et</summary>
        [HttpPost("deactivate")]
        [Authorize]
        [ProducesResponseType(typeof(BaseResponse<string>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(BaseResponse<string>), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Deactivate([FromQuery] Guid gymId)
        {
            var r = await _service.DeactivateAsync(gymId);
            return StatusCode((int)r.StatusCode, r);
        }
    }
}
