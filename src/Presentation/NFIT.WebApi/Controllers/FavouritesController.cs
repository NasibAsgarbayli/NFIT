using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NFIT.Application.Abstracts.Services;
using NFIT.Application.DTOs.FavouriteDtos;
using NFIT.Application.Shared;
using NFIT.Persistence.Services;

namespace NFIT.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FavouritesController : ControllerBase
    {
        private readonly IFavouriteService _service;
        public FavouritesController(IFavouriteService service)
        {
            _service = service;
        }
        [HttpPost]
        [Authorize(Policy = Permissions.Favourite.Create)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] FavouriteAddDto dto)
        {
            var result = await _service.CreateAsync(dto);
            return StatusCode((int)result.StatusCode, result);
        }

        // ---- Delete specific favourite ----
        [HttpDelete("{favouriteId:guid}")]
        [Authorize(Policy = Permissions.Favourite.Delete)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteById(Guid favouriteId)
        {
            var result = await _service.DeleteByIdAsync(favouriteId);
            return StatusCode((int)result.StatusCode, result);
        }

        // ---- Get all my favourites ----
        [HttpGet("MyAllFav")]
        [Authorize]
        [ProducesResponseType(typeof(BaseResponse<List<FavouriteListItemDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<List<FavouriteListItemDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAllMy()
        {
            var result = await _service.GetAllMyFavouriteAsync();
            return StatusCode((int)result.StatusCode, result);
        }


        // ---- Only my favourite gyms ----
        [HttpGet("FavGyms")]
        [Authorize]
        [ProducesResponseType(typeof(BaseResponse<List<FavouriteListItemDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<List<FavouriteListItemDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetGyms()
        {
            var result = await _service.GetUserFavouriteGymsAsync();
            return StatusCode((int)result.StatusCode, result);
        }

        // ---- Only my favourite trainers ----
        [HttpGet("FavTrainers")]
        [Authorize]
        [ProducesResponseType(typeof(BaseResponse<List<FavouriteListItemDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<List<FavouriteListItemDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTrainers()
        {
            var result = await _service.GetUserFavouriteTrainersAsync();
            return StatusCode((int)result.StatusCode, result);
        }

        // ---- Only my favourite supplements ----
        [HttpGet("FavSupplements")]
        [Authorize]
        [ProducesResponseType(typeof(BaseResponse<List<FavouriteListItemDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<List<FavouriteListItemDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetSupplements()
        {
            var result = await _service.GetUserFavouriteSupplementsAsync();
            return StatusCode((int)result.StatusCode, result);
        }

        // ---- Clear all favourites of current user ----
        [HttpDelete("ClearAllFavoruites")]
        [Authorize]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Clear()
        {
            var result = await _service.ClearUserFavouritesAsync();
            return StatusCode((int)result.StatusCode, result);
        }
    }
}
