using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NFIT.Application.Abstracts.Services;
using NFIT.Application.DTOs.ExerciseDtos;
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

        /// <summary> Bütün exercises (soft-deleted olmayanlar) </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var r = await _service.GetAllAsync();
            return StatusCode((int)r.StatusCode, r);
        }

        /// <summary> Id-ə görə exercise </summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var r = await _service.GetByIdAsync(id);
            return StatusCode((int)r.StatusCode, r);
        }

        /// <summary> Ada görə axtarış (contains) </summary>
        [HttpGet("search/by-name")]
        public async Task<IActionResult> GetByName([FromQuery] string name)
        {
            var r = await _service.GetByNameAsync(name);
            return StatusCode((int)r.StatusCode, r);
        }

        /// <summary> MuscleGroup-a görə (primary və ya secondary-də olanlar) </summary>
        [HttpGet("search/by-muscle")]
        public async Task<IActionResult> GetByMuscle([FromQuery] MuscleGroup muscle)
        {
            var r = await _service.GetByMuscleGroupAsync(muscle);
            return StatusCode((int)r.StatusCode, r);
        }

        // ===================== CREATE =====================

        /// <summary> Yeni exercise yarat </summary>
        [Authorize(Policy = "Exercises.Create")] // istəsən sadəcə [Authorize] qoy
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ExerciseCreateDto dto)
        {
            var r = await _service.CreateAsync(dto);
            return StatusCode((int)r.StatusCode, r);
        }

        // ===================== UPDATE =====================

        /// <summary> Exercise yenilə </summary>
        [Authorize(Policy = "Exercises.Update")]
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] ExerciseUpdateDto dto)
        {
            var r = await _service.UpdateAsync(dto);
            return StatusCode((int)r.StatusCode, r);
        }

        // ===================== DELETE =====================

        /// <summary> Exercise sil (soft delete) </summary>
        [Authorize(Policy = "Exercises.Delete")]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var r = await _service.DeleteAsync(id);
            return StatusCode((int)r.StatusCode, r);
        }
    }
}

