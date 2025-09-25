using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NFIT.Application.Abstracts.Services;
using NFIT.Application.DTOs.GymDtos;
using NFIT.Application.DTOs.SearchDtos;
using NFIT.Application.Shared;

namespace NFIT.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        private readonly ISearchService _service;
        public SearchController(ISearchService service)
        {
            _service = service;
        }

        [HttpGet("gyms")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(BaseResponse<List<GymListItemDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<List<GymListItemDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SearchGyms([FromQuery] SearchGymsRequest request)
        {
            var result = await _service.SearchGymsAsync(request);
            return StatusCode((int)result.StatusCode, result);
        }
    }
}
