using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NFIT.Application.Abstracts.Services;
using NFIT.Application.DTOs.SupplementDtos;
using NFIT.Application.Shared;

namespace NFIT.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SupplementsController : ControllerBase
    {
        private readonly ISupplementService _service;
        public SupplementsController(ISupplementService service)
        {
            _service = service;
        }

        // ============ GET ============
        [HttpGet]
        [ProducesResponseType(typeof(BaseResponse<List<SupplementGetDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<List<SupplementGetDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAll([FromQuery] SupplementFilterDto? filter)
            => StatusCode((int)(await _service.GetAllAsync(filter)).StatusCode, await _service.GetAllAsync(filter));

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(BaseResponse<SupplementGetDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<SupplementGetDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
            => StatusCode((int)(await _service.GetByIdAsync(id)).StatusCode, await _service.GetByIdAsync(id));

        [HttpGet("search")]
        [ProducesResponseType(typeof(BaseResponse<List<SupplementGetDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<List<SupplementGetDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SearchByName([FromQuery] string q)
            => StatusCode((int)(await _service.SearchByNameAsync(q)).StatusCode, await _service.SearchByNameAsync(q));

        [HttpGet("brand/{brand}")]
        [ProducesResponseType(typeof(BaseResponse<List<SupplementGetDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<List<SupplementGetDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ByBrand(string brand)
            => StatusCode((int)(await _service.GetByBrandAsync(brand)).StatusCode, await _service.GetByBrandAsync(brand));

        [HttpGet("popular")]
        [ProducesResponseType(typeof(BaseResponse<List<SupplementGetDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<List<SupplementGetDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Popular([FromQuery] int top = 10)
            => StatusCode((int)(await _service.GetPopularSupplementsAsync(top)).StatusCode, await _service.GetPopularSupplementsAsync(top));

        // ============ CREATE ============
        //[Authorize(Policy = Permissions.Supplement.Create)]
        [HttpPost]
        [ProducesResponseType(typeof(BaseResponse<Guid>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(BaseResponse<Guid>), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create([FromBody] SupplementCreateDto dto)
            => StatusCode((int)(await _service.CreateAsync(dto)).StatusCode, await _service.CreateAsync(dto));

        // ============ UPDATE ============
        //[Authorize(Policy = Permissions.Supplement.Update)]
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update(Guid id, [FromBody] SupplementUpdateDto dto)
        {
            dto.Id = id;
            var r = await _service.UpdateAsync(dto);
            return StatusCode((int)r.StatusCode, r);
        }

        [HttpPost("{id:guid}/images")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status201Created)]
        public async Task<IActionResult> AddImage(Guid id, [FromForm] SupplementImageUploadDto dto)
        {
            var r = await _service.AddImageAsync(id, dto);
            return StatusCode((int)r.StatusCode, r);
        }

        // ============ DELETE ============
        //[Authorize(Policy = Permissions.Supplement.Delete)]
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Delete(Guid id)
            => StatusCode((int)(await _service.DeleteAsync(id)).StatusCode, await _service.DeleteAsync(id));

        //[Authorize(Policy = Permissions.Supplement.ManageImages)]
        [HttpDelete("{id:guid}/images/{imageId:guid}")]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteImage(Guid id, Guid imageId)
        {
            var r = await _service.DeleteImageAsync(id, imageId);
            return StatusCode((int)r.StatusCode, r);
        }
    }
}
