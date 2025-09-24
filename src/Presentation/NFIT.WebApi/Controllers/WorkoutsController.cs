using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NFIT.Application.Abstracts.Services;
using NFIT.Application.DTOs.WorkoutDtos;
using NFIT.Domain.Enums;

namespace NFIT.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WorkoutsController : ControllerBase
    {
        private readonly IWorkoutService _service;
        public WorkoutsController(IWorkoutService service)
        {
            _service = service;
        }
        // ===================== GET =====================

        /// <summary>Bütün workout-lar</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var r = await _service.GetAllAsync();
            return StatusCode((int)r.StatusCode, r);
        }

        /// <summary>Id-ə görə workout detalı (exercise xəttləri ilə)</summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var r = await _service.GetByIdAsync(id);
            return StatusCode((int)r.StatusCode, r);
        }

        /// <summary>Kateqoriyaya görə workout-lar</summary>
        [HttpGet("by-category")]
        public async Task<IActionResult> GetByCategory([FromQuery] WorkoutCategory category)
        {
            var r = await _service.GetByCategoryAsync(category);
            return StatusCode((int)r.StatusCode, r);
        }

        // ===================== CREATE =====================

        /// <summary>Yeni workout yarat (içindəki exercise xəttləri ilə birlikdə)</summary>
        // [Authorize(Policy = "Workouts.Create")] // lazımdırsa aç
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] WorkoutCreateDto dto)
        {
            var r = await _service.CreateAsync(dto);
            return StatusCode((int)r.StatusCode, r);
        }

        // ===================== UPDATE =====================

        /// <summary>Workout yenilə (xəttlər tam sinxronlanır)</summary>
        // [Authorize(Policy = "Workouts.Update")]
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] WorkoutUpdateDto dto)
        {
            var r = await _service.UpdateAsync(dto);
            return StatusCode((int)r.StatusCode, r);
        }

        // ===================== DELETE =====================

        /// <summary>Workout sil (soft delete)</summary>
        // [Authorize(Policy = "Workouts.Delete")]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var r = await _service.DeleteAsync(id);
            return StatusCode((int)r.StatusCode, r);
        }
    }
}

