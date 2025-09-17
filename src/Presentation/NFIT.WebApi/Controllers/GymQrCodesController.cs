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
        [HttpGet("active")]
        public async Task<IActionResult> GetActive(Guid gymId)
        {
            var r = await _service.GetActiveAsync(gymId);
            return StatusCode((int)r.StatusCode, r);
        }

        // ======= CREATE/ROTATE =======
        //[Authorize(Policy = Permissions.Gym.ManageQr)]
        [HttpPost("rotate")]
        public async Task<IActionResult> Rotate(Guid gymId)
        {
            var r = await _service.GenerateOrRotateAsync(gymId);
            return StatusCode((int)r.StatusCode, r);
        }

        // ======= DEACTIVATE =======
        //[Authorize(Policy = Permissions.Gym.ManageQr)]
        [HttpPost("deactivate")]
        public async Task<IActionResult> Deactivate(Guid gymId)
        {
            var r = await _service.DeactivateAsync(gymId);
            return StatusCode((int)r.StatusCode, r);
        }
    }
}
