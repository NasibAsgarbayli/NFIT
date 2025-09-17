using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NFIT.Application.Abstracts.Services;
using NFIT.Application.Shared;

namespace NFIT.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MembershipsController : ControllerBase
    {
        private readonly IMembershipService _service;
        public MembershipsController(IMembershipService service)
        {
            _service = service; 
        }
        // ===== GET =====
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetMy()
        {
            var r = await _service.GetMyMembershipAsync();
            return StatusCode((int)r.StatusCode, r);
        }

        [Authorize]
        [HttpGet("me/history")]
        public async Task<IActionResult> GetMyMembershipHistory()
        {
            var r = await _service.GetMyMembershipHistoryAsync();
            return StatusCode((int)r.StatusCode, r);
        }

        // ===== UPDATE =====
        [Authorize]
        [HttpPost("me/cancelmembership")]
        public async Task<IActionResult> CancelMy()
        {
            var r = await _service.CancelMyMembershipAsync();
            return StatusCode((int)r.StatusCode, r);
        }
        //[Authorize(Policy = Permissions.Memberships.Deactivate)]
        [HttpPost("{userId}/deactivate")]
        public async Task<IActionResult> DeactivateUser(string userId)
        {
            var r = await _service.DeactivateUserMembershipAsync(userId);
            return StatusCode((int)r.StatusCode, r);
        }

        // ===== DELETE =====
        [Authorize]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var r = await _service.DeleteAsync(id);
            return StatusCode((int)r.StatusCode, r);
        }

        // ===== ADMIN =====
        //[Authorize(Policy = Permissions.Memberships.ViewUser)] // öz policy adınla əvəz et
        [HttpGet("users/{userId}")]
        public async Task<IActionResult> GetForUser(string userId)
        {
            var r = await _service.GetUsersMembershipAsync(userId);
            return StatusCode((int)r.StatusCode, r);
        }
    }
}
