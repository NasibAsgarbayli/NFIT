using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NFIT.Application.Abstracts.Services;
using NFIT.Application.DTOs.MembershipDtos;
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
        // ========== USER (öz məlumatları) ==========

        /// <summary>Delivered subscription order əsasənda membership YARAT</summary>
        [Authorize]
        [HttpPost("from-order")]
        [ProducesResponseType(typeof(BaseResponse<Guid>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(BaseResponse<Guid>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(BaseResponse<Guid>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(BaseResponse<Guid>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(BaseResponse<Guid>), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateFromOrder([FromBody] MembershipCreateFromOrderDto dto)
        {
            var r = await _service.CreateFromDeliveredOrderAsync(dto);
            return StatusCode((int)r.StatusCode, r);
        }

        /// <summary>Hal-hazırda aktiv olan üzvlüyü qaytarır</summary>
        [Authorize]
        [HttpGet("me")]
        [ProducesResponseType(typeof(BaseResponse<MembershipGetDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<MembershipGetDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMy()
        {
            var r = await _service.GetMyMembershipAsync();
            return StatusCode((int)r.StatusCode, r);
        }

        /// <summary>Öz üzvlük tarixçəsi</summary>
        [Authorize]
        [HttpGet("me/history")]
        [ProducesResponseType(typeof(BaseResponse<List<MembershipListItemDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<List<MembershipListItemDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMyMembershipHistory()
        {
            var r = await _service.GetMyMembershipHistoryAsync();
            return StatusCode((int)r.StatusCode, r);
        }

        /// <summary>Öz aktiv üzvlüyünü ləğv et</summary>
        [Authorize]
        [HttpPost("me/cancelmembership")]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CancelMy()
        {
            var r = await _service.CancelMyMembershipAsync();
            return StatusCode((int)r.StatusCode, r);
        }

        // ========== ADMIN / MODERATOR əməliyyatları ==========

        /// <summary>İstifadəçinin aktiv üzvlüyünü deaktiv et</summary>
        [Authorize(Policy = Permissions.Membership.DeactivateUser)]
        [HttpPost("{userId}/deactivate")]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeactivateUser(string userId)
        {
            var r = await _service.DeactivateUserMembershipAsync(userId);
            return StatusCode((int)r.StatusCode, r);
        }

        /// <summary>İstənilən üzvlüyü sil (soft delete)</summary>
        [Authorize(Policy = Permissions.Membership.Delete)]
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var r = await _service.DeleteAsync(id);
            return StatusCode((int)r.StatusCode, r);
        }

        /// <summary>İstifadəçinin üzvlük məlumatlarını əldə et</summary>
        [Authorize(Policy = Permissions.Membership.ViewUser)]
        [HttpGet("users/{userId}")]
        [ProducesResponseType(typeof(BaseResponse<MembershipGetDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<MembershipGetDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetForUser(string userId)
        {
            var r = await _service.GetUsersMembershipAsync(userId);
            return StatusCode((int)r.StatusCode, r);
        }
    }
}


