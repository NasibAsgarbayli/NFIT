using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NFIT.Application.Abstracts.Services;
using NFIT.Application.DTOs.ExerciseDtos;
using NFIT.Application.Shared;
using NFIT.Domain.Enums;

namespace NFIT.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExercisesController : ControllerBase
    {
        private readonly IExerciseService _service;
        public ExercisesController(IExerciseService service)
        {
            _service = service;
     
        }
        // ===================== GET =====================

        /// <summary>Bütün exercises (soft-deleted olmayanlar)</summary>
        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(BaseResponse<List<ExerciseGetDto>>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(BaseResponse<List<ExerciseGetDto>>), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>Id-ə görə exercise</summary>
        [HttpGet("{id:guid}")]
        [Authorize]
        [ProducesResponseType(typeof(BaseResponse<ExerciseGetDto>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(BaseResponse<ExerciseGetDto>), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>Ada görə axtarış (contains)</summary>
        [HttpGet("search/by-name")]
        [Authorize]
        [ProducesResponseType(typeof(BaseResponse<List<ExerciseGetDto>>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(BaseResponse<List<ExerciseGetDto>>), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetByName([FromQuery] string name)
        {
            var result = await _service.GetByNameAsync(name);
            return StatusCode((int)result.StatusCode, result);
        }

        /// <summary>MuscleGroup-a görə (primary və ya secondary)</summary>
        [HttpGet("search/by-muscle")]
        [Authorize]
        [ProducesResponseType(typeof(BaseResponse<List<ExerciseGetDto>>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(BaseResponse<List<ExerciseGetDto>>), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetByMuscle([FromQuery] MuscleGroup muscle)
        {
            var result = await _service.GetByMuscleGroupAsync(muscle);
            return StatusCode((int)result.StatusCode, result);
        }

        // ===================== CREATE =====================

        /// <summary>Yeni exercise yarat</summary>
        [HttpPost]
        [Authorize(Policy = Permissions.Exercise.Create)]
        [ProducesResponseType(typeof(BaseResponse<Guid>), (int)HttpStatusCode.Created)]
        [ProducesResponseType(typeof(BaseResponse<Guid>), (int)HttpStatusCode.Conflict)]
        public async Task<IActionResult> Create([FromBody] ExerciseCreateDto dto)
        {
            var result = await _service.CreateAsync(dto);
            return StatusCode((int)result.StatusCode, result);
        }

        // ===================== UPDATE =====================

        /// <summary>Exercise yenilə</summary>
        [HttpPut]
        [Authorize(Policy = Permissions.Exercise.Update)]
        [ProducesResponseType(typeof(BaseResponse<string>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(BaseResponse<string>), (int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(BaseResponse<string>), (int)HttpStatusCode.Conflict)]
        public async Task<IActionResult> Update([FromBody] ExerciseUpdateDto dto)
        {
            var result = await _service.UpdateAsync(dto);
            return StatusCode((int)result.StatusCode, result);
        }

        // ===================== DELETE =====================

        /// <summary>Exercise sil (soft delete)</summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Policy = Permissions.Exercise.Delete)]
        [ProducesResponseType(typeof(BaseResponse<string>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(BaseResponse<string>), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _service.DeleteAsync(id);
            return StatusCode((int)result.StatusCode, result);
        }
    }
}

