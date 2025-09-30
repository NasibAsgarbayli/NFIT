using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NFIT.Application.Abstracts.Services;
using NFIT.Application.DTOs.SubscriptionPlanDtos;
using NFIT.Application.Shared;

namespace NFIT.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubscriptionPlansControllers : ControllerBase
    {
        private readonly ISubscriptionPlanService _service;
        public SubscriptionPlansControllers(ISubscriptionPlanService service)
        {
            _service = service;
        }

        [HttpPost]
        [Authorize(Policy =Permissions.Subscription.Create)]
        [ProducesResponseType(typeof(BaseResponse<Guid>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(BaseResponse<Guid>), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create([FromBody] SubscriptionPlanCreateDto dto)
        {
            var r = await _service.CreateAsync(dto);
            return StatusCode((int)r.StatusCode, r);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Policy = Permissions.Subscription.Update)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Update(Guid id, [FromBody] SubscriptionPlanUpdateDto dto)
        {
            dto.Id = id;
            var r = await _service.UpdateAsync(dto);
            return StatusCode((int)r.StatusCode, r);
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Policy = Permissions.Subscription.Delete)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var r = await _service.DeleteAsync(id);
            return StatusCode((int)r.StatusCode, r);
        }

        [HttpGet("Get All")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(BaseResponse<List<SubscriptionPlanGetDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<List<SubscriptionPlanGetDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAll()
        {
            var r = await _service.GetAllAsync();
            return StatusCode((int)r.StatusCode, r);
        }

        [HttpGet("{id:guid}")]
        [Authorize]
        [ProducesResponseType(typeof(BaseResponse<SubscriptionPlanGetDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<SubscriptionPlanGetDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var r = await _service.GetByIdAsync(id);
            return StatusCode((int)r.StatusCode, r);
        }

        [HttpGet("by-name")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(BaseResponse<SubscriptionPlanGetDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<SubscriptionPlanGetDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByName([FromQuery] string name)
        {
            var r = await _service.GetByNameAsync(name);
            return StatusCode((int)r.StatusCode, r);
        }
        /// <summary>Ən ucuz plan</summary>
        [HttpGet("cheapest")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(BaseResponse<SubscriptionPlanGetDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<SubscriptionPlanGetDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCheapest()
        {
            var r = await _service.GetCheapestPlanAsync();
            return StatusCode((int)r.StatusCode, r);
        }

        /// <summary>Ən bahalı plan</summary>
        [HttpGet("most-expensive")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(BaseResponse<SubscriptionPlanGetDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<SubscriptionPlanGetDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMostExpensive()
        {
            var r = await _service.GetMostExpensivePlanAsync();
            return StatusCode((int)r.StatusCode, r);
        }

        /// <summary>Planları gym sayına görə sırala</summary>
        /// <param name="descending">default: true (azalan). false versən – artan qaytarar</param>
        [HttpGet("by-gym-count")]
        [Authorize]
        [ProducesResponseType(typeof(BaseResponse<List<SubscriptionPlanGetDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<List<SubscriptionPlanGetDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByGymCount([FromQuery] bool descending = true)
        {
            var r = await _service.GetPlansByGymCountAsync(descending);
            return StatusCode((int)r.StatusCode, r);
        }
    }
}
