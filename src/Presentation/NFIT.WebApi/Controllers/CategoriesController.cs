using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NFIT.Application.Abstracts.Services;
using NFIT.Application.DTOs.CategoryDtos;
using NFIT.Application.Shared;

namespace NFIT.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService _service;

        public CategoriesController(ICategoryService service)
        {
            _service = service;
        }

        [HttpPost]
        [ProducesResponseType(typeof(BaseResponse<Guid>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(BaseResponse<Guid>), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create([FromBody] CategoryCreateDto dto)
        {
            var result = await _service.CreateAsync(dto);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] CategoryUpdateDto dto)
        {
            dto.Id = id;
            var result = await _service.UpdateAsync(dto);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete([FromRoute] Guid id)
        {
            var result = await _service.DeleteAsync(id);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(BaseResponse<CategoryGetDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<CategoryGetDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpGet]
        [ProducesResponseType(typeof(BaseResponse<List<CategoryGetDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<List<CategoryGetDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            return StatusCode((int)result.StatusCode, result);
        }
        /// <summary>Ada görə category tap</summary>
        [HttpGet("by-name")]
        [ProducesResponseType(typeof(BaseResponse<CategoryGetDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<CategoryGetDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByName([FromQuery] string name)
        {
            var result = await _service.GetByNameAsync(name);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>ID ilə category-ni, bağlı gym-larla birlikdə gətir</summary>
        [HttpGet("{id:guid}/with-gyms")]
        [ProducesResponseType(typeof(BaseResponse<CategoryWithGymsDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<CategoryWithGymsDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByIdWithGyms([FromRoute] Guid id)
        {
            var result = await _service.GetByIdWithGymsAsync(id);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>Aktiv category sayı</summary>
        [HttpGet("count")]
        [ProducesResponseType(typeof(BaseResponse<int>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCount()
        {
            var result = await _service.GetCategoryCountAsync();
            return StatusCode((int)result.StatusCode, result);
        }
    }
}

