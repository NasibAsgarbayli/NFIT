using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NFIT.Application.Abstracts.Services;
using NFIT.Application.DTOs.ReviewDtos;
using NFIT.Application.Shared;

namespace NFIT.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReviewsController : ControllerBase
    {
        private readonly IReviewService _service;
        public ReviewsController(IReviewService service)
        {
            _service = service;
        }
        // ---------- Create ----------
        [Authorize]
        [HttpPost]
        [ProducesResponseType(typeof(BaseResponse<Guid>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(BaseResponse<Guid>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(BaseResponse<Guid>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(BaseResponse<Guid>), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create([FromBody] ReviewCreateDto dto)
        {
            var r = await _service.CreateAsync(dto);
            return StatusCode((int)r.StatusCode, r);
        }

        // ---------- Update (only owner) ----------
        [Authorize]
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] ReviewUpdateDto dto)
        {
            dto.Id = id;
            var r = await _service.UpdateAsync(dto);
            return StatusCode((int)r.StatusCode, r);
        }

        // ---------- Delete by Id (owner; admin/mod üçün ayrıca policy varsa onu istifadə et) ----------
        [Authorize]
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete([FromRoute] Guid id)
        {
            var r = await _service.DeleteByIdAsync(id);
            return StatusCode((int)r.StatusCode, r);
        }

        // ---------- Approve (permission-based) ----------
        // NOTE: Permissions.Review.Approve policy-ni Program.cs-də əlavə edəcəksən.
       
        [HttpPost("{id:guid}/approve")]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Approve([FromRoute] Guid id)
        {
            var r = await _service.ApproveReviewAsync(id);
            return StatusCode((int)r.StatusCode, r);
        }

        // ---------- Public: Approved reviews (filter + pagination) ----------
        [HttpGet("ApprovedReviews")]
        [ProducesResponseType(typeof(BaseResponse<List<ReviewGetDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<List<ReviewGetDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetApproved([FromQuery] ReviewQueryDto query)
        {
            var r = await _service.GetApprovedReviewsAsync(query);
            return StatusCode((int)r.StatusCode, r);
        }

        // ---------- Public: Average rating (only approved) ----------
        [HttpGet("average-rating")]
        [ProducesResponseType(typeof(BaseResponse<decimal>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<decimal>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(BaseResponse<decimal>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAverage([FromQuery] ReviewQueryDto query)
        {
            var r = await _service.GetAverageRatingAsync(query);
            return StatusCode((int)r.StatusCode, r);
        }

        // ---------- Current user's reviews ----------
        [Authorize]
        [HttpGet("MyReviews")]
        [ProducesResponseType(typeof(BaseResponse<List<ReviewGetDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<List<ReviewGetDto>>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(BaseResponse<List<ReviewGetDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMine()
        {
            var r = await _service.GetMyReviewsAsync();
            return StatusCode((int)r.StatusCode, r);
        }

        // ---------- Has current user already reviewed this target? ----------
        [Authorize]
        [HttpGet("has-reviewed")]
        [ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> HasReviewed([FromQuery] ReviewCreateDto dto)
        {
            var r = await _service.HasUserReviewedAsync(dto);
            return StatusCode((int)r.StatusCode, r);
        }

        // ---------- Clear all my reviews ----------
        [Authorize]
        [HttpDelete("clear")]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ClearAllMyReviews()
        {
            var r = await _service.ClearAllMyReviewsAsync();
            return StatusCode((int)r.StatusCode, r);
        }
    }
}
