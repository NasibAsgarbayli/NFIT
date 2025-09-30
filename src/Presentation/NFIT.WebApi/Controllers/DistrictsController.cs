using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NFIT.Application.Abstracts.Services;
using NFIT.Application.DTOs.DistrictDtos;
using NFIT.Application.Shared;

namespace NFIT.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DistrictsController : ControllerBase
    {
        private readonly IDistrictService _service;
        public DistrictsController(IDistrictService service)
        {
          _service = service;  
        }

        // ==================== GET ====================

        /// <summary>Aktiv rayonların siyahısı</summary>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(BaseResponse<List<DistrictGetDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<List<DistrictGetDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAll()
        {
            var r = await _service.GetAllAsync();
            return StatusCode((int)r.StatusCode, r);
        }



        /// <summary>Rayondakı aktiv gym sayını gətirir</summary>
        [HttpGet("{id:guid}/gym-count")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(BaseResponse<int>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<int>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetGymCount(Guid id)
        {
            var r = await _service.GetGymCountByDistrictAsync(id);
            return StatusCode((int)r.StatusCode, r);
        }

        // ==================== CREATE ====================

        /// <summary>Yeni rayon əlavə et</summary>
        //[Authorize(Policy = Permissions.District.Create)]
        [HttpPost]
        [Authorize(Policy = Permissions.District.Create)]
        [ProducesResponseType(typeof(BaseResponse<Guid>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(BaseResponse<Guid>), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(BaseResponse<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] DistrictCreateDto dto)
        {
            var r = await _service.CreateAsync(dto);
            return StatusCode((int)r.StatusCode, r);
        }

        // ==================== UPDATE ====================

        /// <summary>Rayon məlumatını yenilə</summary>
        //[Authorize(Policy = Permissions.District.Update)]
        [HttpPut("{id:guid}")]
        [Authorize(Policy = Permissions.District.Update)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Update(Guid id, [FromBody] DistrictUpdateDto dto)
        {
            dto.Id = id;
            var r = await _service.UpdateAsync(dto);
            return StatusCode((int)r.StatusCode, r);
        }

        /// <summary>Rayonu deaktiv et (seçilə bilməz)</summary>
        //[Authorize(Policy = Permissions.District.Deactivate)]
        [HttpPost("{id:guid}/deactivate")]
        [Authorize(Policy = Permissions.District.Deactivate)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Deactivate(Guid id)
        {
            var r = await _service.DeactivateDistrictAsync(id);
            return StatusCode((int)r.StatusCode, r);
        }
        /// <summary>Rayonu aktiv et</summary>
        //[Authorize(Policy = Permissions.District.Activate)]
        [HttpPost("{id:guid}/activate")]
        [Authorize(Policy = Permissions.District.Activate)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Activate(Guid id)
        {
            var r = await _service.ActivateDistrictAsync(id);
            return StatusCode((int)r.StatusCode, r);
        }
        // ==================== DELETE ====================

        /// <summary>Rayonu sil (soft delete + deaktivasiya)</summary>
        //[Authorize(Policy = Permissions.District.Delete)]
        [HttpDelete("{id:guid}")]
        [Authorize(Policy = Permissions.District.Delete)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var r = await _service.DeleteAsync(id);
            return StatusCode((int)r.StatusCode, r);
        }
    }
}
