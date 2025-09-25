using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NFIT.Application.Abstracts.Services;
using NFIT.Application.DTOs.GymDtos;
using NFIT.Application.Shared;

namespace NFIT.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GymsController : ControllerBase
    {
        private readonly IGymService _gymService;

        public GymsController(IGymService gymService)
        {
            _gymService = gymService;
        }

        /// <summary>Create a new gym</summary>
        [HttpPost]
        [Authorize(Policy =Permissions.Gym.Create)]
        [ProducesResponseType(typeof(BaseResponse<Guid>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(BaseResponse<Guid>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(BaseResponse<Guid>), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create([FromBody] GymCreateDto dto)
        {
            var result = await _gymService.CreateAsync(dto);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPost("{id:guid}/images")]
        [Consumes("multipart/form-data")]               
        [RequestSizeLimit(20_000_000)]
        [Authorize(Policy = Permissions.Gym.AddImage)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AddImage([FromRoute] Guid id, [FromForm] GymImageAddRequest request)
        {
            var result = await _gymService.AddImageAsync(id, request.Image);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>Update an existing gym</summary>


        /// <summary>Soft delete a gym</summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Policy = Permissions.Gym.Delete)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]

        public async Task<IActionResult> Delete([FromRoute] Guid id)
        {
            var result = await _gymService.DeleteAsync(id);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpDelete("{id:guid}/images/{imageId:guid}")]
        [Authorize(Policy = Permissions.Gym.DeleteImage)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteImage([FromRoute] Guid id, [FromRoute] Guid imageId)
        {
            var result = await _gymService.DeleteImageAsync(id, imageId);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>Get gym details by id</summary>
        [HttpGet("{id:guid}")]
        [Authorize]
        [ProducesResponseType(typeof(BaseResponse<GymDetailsDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<GymDetailsDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var result = await _gymService.GetByIdAsync(id);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>Get paginated list of gyms</summary>
        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(BaseResponse<List<GymListItemDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<List<GymListItemDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _gymService.GetAllAsync(page, pageSize);
            return StatusCode((int)result.StatusCode, result);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Policy = Permissions.Gym.Update)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] GymUpdateDto dto)
        {
            dto.Id = id;
            var result = await _gymService.UpdateAsync(dto);
            return StatusCode((int)result.StatusCode, result);
        }
    }
}

