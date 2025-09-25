using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using NFIT.Application.Abstracts.Services;
using NFIT.Application.DTOs.FileUploadDto;
using NFIT.Application.DTOs.TrainerDtos;
using NFIT.Application.Shared;
using NFIT.Domain.Enums;
using NFIT.Persistence.Services;

namespace NFIT.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TrainersController : ControllerBase
    {
        private readonly ITrainerService _svc;
        public TrainersController(ITrainerService svc)
        {
            _svc = svc;

        }

        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(BaseResponse<List<TrainerListItemDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<List<TrainerListItemDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAll([FromQuery] TrainerFilterDto filter)
        {
            var res = await _svc.GetAllAsync(filter);
            return StatusCode((int)res.StatusCode, res);
        }

        [HttpGet("{id:guid}")]
        [Authorize]
        [ProducesResponseType(typeof(BaseResponse<TrainerGetDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<TrainerGetDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var res = await _svc.GetByIdAsync(id);
            return StatusCode((int)res.StatusCode, res);
        }

        [HttpGet("top")]
        [Authorize]
        [ProducesResponseType(typeof(BaseResponse<List<TrainerListItemDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<List<TrainerListItemDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTop([FromQuery] int top = 10)
        {
            var res = await _svc.GetTopAsync(top);
            return StatusCode((int)res.StatusCode, res);
        }

        [HttpGet("search")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(BaseResponse<List<TrainerListItemDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<List<TrainerListItemDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByName([FromQuery] string name)
        {
            var res = await _svc.GetByNameAsync(name);
            return StatusCode((int)res.StatusCode, res);
        }

        // ===================== TRAINER: C/U/D =====================
        [HttpPost]
        [Authorize(Policy =Permissions.Trainer.Create)]
        [ProducesResponseType(typeof(BaseResponse<Guid>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(BaseResponse<Guid>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(BaseResponse<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] TrainerCreateDto dto)
        {
            var res = await _svc.CreateAsync(dto);
            return StatusCode((int)res.StatusCode, res);
        }

        [HttpPut]
        [Authorize(Policy = Permissions.Trainer.Update)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update([FromBody] TrainerUpdateDto dto)
        {
            var res = await _svc.UpdateAsync(dto);
            return StatusCode((int)res.StatusCode, res);
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Policy = Permissions.Trainer.Delete)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var res = await _svc.DeleteAsync(id);
            return StatusCode((int)res.StatusCode, res);
        }

        // ===================== Verify / Active =====================
        [HttpPost("{id:guid}/verify")]
        [Authorize(Policy = Permissions.Trainer.Verify)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Verify(Guid id)
        {
            var res = await _svc.VerifyAsync(id);
            return StatusCode((int)res.StatusCode, res);
        }

        [HttpPost("{id:guid}/toggle-active")]
        [Authorize(Policy = Permissions.Trainer.ToggleActive)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ToggleActive(Guid id, [FromQuery] bool isActive)
        {
            var res = await _svc.ToggleActiveAsync(id, isActive);
            return StatusCode((int)res.StatusCode, res);
        }

        // ===================== TRAINER IMAGES =====================
        [HttpPost("{trainerId:guid}/images")]
        [Consumes("multipart/form-data")]
        [Authorize(Policy = Permissions.Trainer.AddImageAsync)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AddImageAsync(
            Guid trainerId,
            [FromForm] TrainerImageUploadPhotoDto dto)
        {
            var res = await _svc.AddImageAsync(trainerId, dto);
            return StatusCode((int)res.StatusCode, res);
        }

        [HttpDelete("{trainerId:guid}/images/{imageId:guid}")]
        [Authorize(Policy = Permissions.Trainer.DeleteImageAsync)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteImageAsync(Guid trainerId, Guid imageId)
        {
            var res = await _svc.DeleteImageAsync(trainerId, imageId);
            return StatusCode((int)res.StatusCode, res);
        }

        // ===================== VIDEOS =====================
        [HttpGet("videos/searchbyname")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(BaseResponse<List<TrainerVideoListItemDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<List<TrainerVideoListItemDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetVideosByName([FromQuery] string name)
        {
            var res = await _svc.GetVideosByNameAsync(name);
            return StatusCode((int)res.StatusCode, res);
        }

        [HttpGet("{trainerId:guid}/videos")]
        [Authorize]
        [ProducesResponseType(typeof(BaseResponse<List<TrainerVideoListItemDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<List<TrainerVideoListItemDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTrainerVideos(Guid trainerId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var res = await _svc.GetTrainerVideosAsync(trainerId, page, pageSize);
            return StatusCode((int)res.StatusCode, res);
        }

        [HttpGet("videos/{videoId:guid}")]
        [Authorize]
        [ProducesResponseType(typeof(BaseResponse<TrainerVideoGetDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<TrainerVideoGetDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetVideoById(Guid videoId)
        {
            var res = await _svc.GetVideoByIdAsync(videoId);
            return StatusCode((int)res.StatusCode, res);
        }

        [HttpGet("videos/feed")]
        [Authorize]
        [ProducesResponseType(typeof(BaseResponse<List<TrainerVideoListItemDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<List<TrainerVideoListItemDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetVideoFeed([FromQuery] TrainerVideoFeedFilterDto filter)
        {
            var res = await _svc.GetVideoFeedAsync(filter);
            return StatusCode((int)res.StatusCode, res);
        }

        [HttpGet("videos/recent")]
        [Authorize]
        [ProducesResponseType(typeof(BaseResponse<List<TrainerVideoListItemDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<List<TrainerVideoListItemDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetRecentVideos([FromQuery] int days = 30, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var res = await _svc.GetRecentVideosAsync(days, page, pageSize);
            return StatusCode((int)res.StatusCode, res);
        }

        [HttpGet("videos/popular")]
        [Authorize]
        [ProducesResponseType(typeof(BaseResponse<List<TrainerVideoListItemDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<List<TrainerVideoListItemDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPopularVideos([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var res = await _svc.GetPopularVideosAsync(page, pageSize);
            return StatusCode((int)res.StatusCode, res);
        }

        [HttpPost("videos")]
        [Authorize(Policy = Permissions.Trainer.CreateVideo)]
        [ProducesResponseType(typeof(BaseResponse<Guid>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(BaseResponse<Guid>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(BaseResponse<Guid>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateVideo([FromBody] TrainerVideoCreateDto dto)
        {
            var res = await _svc.CreateVideoAsync(dto);
            return StatusCode((int)res.StatusCode, res);
        }

        [HttpPut("videos")]
        [Authorize(Policy = Permissions.Trainer.UpdateVideo)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateVideo([FromBody] TrainerVideoUpdateDto dto)
        {
            var res = await _svc.UpdateVideoAsync(dto);
            return StatusCode((int)res.StatusCode, res);
        }

        [HttpDelete("videos/{videoId:guid}")]
        [Authorize(Policy = Permissions.Trainer.DeleteVideo)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteVideo(Guid videoId)
        {
            var res = await _svc.DeleteVideoAsync(videoId);
            return StatusCode((int)res.StatusCode, res);
        }

        [HttpPost("videos/{videoId:guid}/upload")]
        [Consumes("multipart/form-data")]
        [Authorize(Policy = Permissions.Trainer.UploadVideo)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UploadVideo(Guid videoId, [FromForm] FileUploadDto dto)
        {
            var res = await _svc.UploadVideoFileAsync(videoId, dto.File);
            return StatusCode((int)res.StatusCode, res);
        }

        [HttpPost("videos/{videoId:guid}/thumbnail")]
        [Consumes("multipart/form-data")]
        [Authorize(Policy = Permissions.Trainer.UploadVideoThumb)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UploadVideoThumb(Guid videoId, [FromForm] FileUploadDto dto)
        {
            var res = await _svc.UploadVideoThumbnailAsync(videoId, dto.File);
            return StatusCode((int)res.StatusCode, res);
        }

        // ===================== WORKOUTS =====================
        [HttpGet("{trainerId:guid}/workouts")]
        [Authorize]
        [ProducesResponseType(typeof(BaseResponse<List<TrainerWorkoutListItemDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<List<TrainerWorkoutListItemDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTrainerWorkouts(Guid trainerId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var res = await _svc.GetTrainerWorkoutsAsync(trainerId, page, pageSize);
            return StatusCode((int)res.StatusCode, res);
        }

        [HttpGet("workouts/{workoutId:guid}")]
        [Authorize]
        [ProducesResponseType(typeof(BaseResponse<TrainerWorkoutGetDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<TrainerWorkoutGetDto>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetWorkoutById(Guid workoutId)
        {
            var res = await _svc.GetWorkoutByIdAsync(workoutId);
            return StatusCode((int)res.StatusCode, res);
        }

        [HttpGet("workouts/feed")]
        [Authorize]
        [ProducesResponseType(typeof(BaseResponse<List<TrainerWorkoutListItemDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<List<TrainerWorkoutListItemDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetWorkoutFeed([FromQuery] TrainerWorkoutFilterDto filter)
        {
            var res = await _svc.GetWorkoutFeedAsync(filter);
            return StatusCode((int)res.StatusCode, res);
        }

        [HttpGet("workouts/searchbyname")]
        [Authorize]
        [ProducesResponseType(typeof(BaseResponse<List<TrainerWorkoutListItemDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<List<TrainerWorkoutListItemDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetWorkoutsByName([FromQuery] string name)
        {
            var res = await _svc.GetWorkoutsByNameAsync(name);
            return StatusCode((int)res.StatusCode, res);
        }

        [HttpGet("workouts/by-difficulty")]
        [Authorize]
        [ProducesResponseType(typeof(BaseResponse<List<TrainerWorkoutListItemDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<List<TrainerWorkoutListItemDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetWorkoutsByDifficulty([FromQuery] DifficultyLevel level,
            [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var res = await _svc.GetWorkoutsByDifficultyAsync(level, page, pageSize);
            return StatusCode((int)res.StatusCode, res);
        }

        [HttpGet("workouts/by-category")]
        [Authorize]
        [ProducesResponseType(typeof(BaseResponse<List<TrainerWorkoutListItemDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<List<TrainerWorkoutListItemDto>>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetWorkoutsByCategory([FromQuery] WorkoutCategory category,
            [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var res = await _svc.GetWorkoutsByCategoryAsync(category, page, pageSize);
            return StatusCode((int)res.StatusCode, res);
        }

        [HttpPost("workouts")]
        [Authorize(Policy = Permissions.Trainer.CreateWorkout)]
        [ProducesResponseType(typeof(BaseResponse<Guid>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(BaseResponse<Guid>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(BaseResponse<Guid>), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateWorkout([FromBody] TrainerWorkoutCreateDto dto)
        {
            var res = await _svc.CreateWorkoutAsync(dto);
            return StatusCode((int)res.StatusCode, res);
        }

        [HttpPut("workouts")]
        [Authorize(Policy = Permissions.Trainer.UpdateWorkout)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateWorkout([FromBody] TrainerWorkoutUpdateDto dto)
        {
            var res = await _svc.UpdateWorkoutAsync(dto);
            return StatusCode((int)res.StatusCode, res);
        }

        [HttpDelete("workouts/{workoutId:guid}")]
        [Authorize(Policy = Permissions.Trainer.DeleteWorkout)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteWorkout(Guid workoutId)
        {
            var res = await _svc.DeleteWorkoutAsync(workoutId);
            return StatusCode((int)res.StatusCode, res);
        }

        [HttpPost("workouts/{workoutId:guid}/thumbnail")]
        [Consumes("multipart/form-data")]
        [Authorize(Policy = Permissions.Trainer.UploadWorkoutThumb)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UploadWorkoutThumb(Guid workoutId, [FromForm] FileUploadDto dto)
        {
            var res = await _svc.UploadWorkoutThumbnailAsync(workoutId, dto.File);
            return StatusCode((int)res.StatusCode, res);
        }

        [HttpPost("workouts/{workoutId:guid}/preview")]
        [Consumes("multipart/form-data")]
        [Authorize(Policy = Permissions.Trainer.UploadWorkoutPreview)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UploadWorkoutPreview(Guid workoutId, [FromForm] FileUploadDto dto)
        {
            var res = await _svc.UploadWorkoutPreviewAsync(workoutId, dto.File);
            return StatusCode((int)res.StatusCode, res);
        }
    }
}
