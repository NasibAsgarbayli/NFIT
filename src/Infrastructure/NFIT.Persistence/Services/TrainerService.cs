using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NFIT.Application.Abstracts.Services;
using NFIT.Application.DTOs.SupplementDtos;
using NFIT.Application.DTOs.TrainerDtos;
using NFIT.Application.Shared;
using NFIT.Domain.Entities;
using NFIT.Domain.Enums;
using NFIT.Infrastructure.Services;
using NFIT.Persistence.Contexts;

namespace NFIT.Persistence.Services;

public class TrainerService:ITrainerService
{
    private readonly NFITDbContext _ctx;
    private readonly IFileService _files;
    private readonly IHttpContextAccessor _http;
    private readonly IAuthorizationService _auth;

    public TrainerService(NFITDbContext ctx, IFileService files, IHttpContextAccessor http,IAuthorizationService auth)
    {
        _ctx = ctx; 
        _files = files; 
        _http = http;
        _auth = auth;
    }

    private ClaimsPrincipal? User => _http.HttpContext?.User;

    private string? CurrentUserId =>
        User?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User?.FindFirst("sub")?.Value;

    private async Task<bool> CanAsync(string policyName)
        => User is not null && (await _auth.AuthorizeAsync(User, policyName)).Succeeded;

    private async Task<bool> IsOwnerAsync(Guid trainerId)
        => await _ctx.Trainers
            .Where(t => t.Id == trainerId && !t.IsDeleted)
            .Select(t => t.UserId == CurrentUserId)
            .FirstOrDefaultAsync();

    // ================== TRAINER ==================
    public async Task<BaseResponse<Guid>> CreateAsync(TrainerCreateDto dto)
    {
        var userId = dto.UserId ?? CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
            return new("Unauthorized", Guid.Empty, HttpStatusCode.Unauthorized);

        var id = Guid.NewGuid();
        var t = new Trainer
        {
            Id = id,
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Bio = dto.Bio?.Trim() ?? "",
            Specializations = dto.Specializations ?? Array.Empty<string>(),
            Certifications = dto.Certifications ?? Array.Empty<string>(),
            ExperienceYears = dto.ExperienceYears,
            InstagramUrl = dto.InstagramUrl?.Trim(),
            YoutubeUrl = dto.YoutubeUrl?.Trim(),
     
            Rating = 0m,
            TotalRatings = 0,
            IsVerified = false,
            IsActive = true,
            UserId = userId
        };

        await _ctx.Trainers.AddAsync(t);
        await _ctx.SaveChangesAsync();
        return new("Trainer created", id, HttpStatusCode.Created);
    }

    public async Task<BaseResponse<List<TrainerListItemDto>>> GetByNameAsync(string name)
    {
        var s = name.Trim().ToLower();
        var list = await _ctx.Trainers.AsNoTracking()
            .Where(t => !t.IsDeleted && (t.FirstName + " " + t.LastName).ToLower().Contains(s))
            .Select(t => new TrainerListItemDto
            {
                Id = t.Id,
                FullName = t.FirstName + " " + t.LastName,
               
                Rating = t.Rating,
                TotalRatings = t.TotalRatings,
                IsVerified = t.IsVerified,
                IsActive = t.IsActive
            })
            .ToListAsync();

        if (list.Count == 0) return new("No trainers found", null, HttpStatusCode.NotFound);
        return new("Trainers found", list, HttpStatusCode.OK);
    }

    public async Task<BaseResponse<string>> UpdateAsync(TrainerUpdateDto dto)
    {
        var t = await _ctx.Trainers.FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted);
        if (t is null) return new("Trainer not found", HttpStatusCode.NotFound);

        var owner = await IsOwnerAsync(dto.Id);
        var canModerate = await CanAsync("Trainers.Moderate");
        if (!(owner || canModerate)) return new("Forbidden", HttpStatusCode.Forbidden);

        t.FirstName = dto.FirstName.Trim();
        t.LastName = dto.LastName.Trim();
        t.Bio = dto.Bio?.Trim() ?? "";
        t.Specializations = dto.Specializations ?? Array.Empty<string>();
        t.Certifications = dto.Certifications ?? Array.Empty<string>();
        t.ExperienceYears = dto.ExperienceYears;
        t.InstagramUrl = dto.InstagramUrl?.Trim();
        t.YoutubeUrl = dto.YoutubeUrl?.Trim();
        t.IsActive = dto.IsActive;

        if (canModerate) t.IsVerified = dto.IsVerified; // yalnız policy-si keçənlər
        t.UpdatedAt = DateTime.UtcNow;

        _ctx.Trainers.Update(t);
        await _ctx.SaveChangesAsync();
        return new("Trainer updated", HttpStatusCode.OK);
    }

    public async Task<BaseResponse<string>> DeleteAsync(Guid id)
    {
        var t = await _ctx.Trainers.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (t is null) return new("Trainer not found", HttpStatusCode.NotFound);

        var owner = await IsOwnerAsync(id);
        var canModerate = await CanAsync("Trainers.Moderate");
        if (!(owner || canModerate)) return new("Forbidden", HttpStatusCode.Forbidden);

        t.IsDeleted = true;
        t.IsActive = false;
        t.UpdatedAt = DateTime.UtcNow;
        _ctx.Trainers.Update(t);
        await _ctx.SaveChangesAsync();
        return new("Trainer deleted", HttpStatusCode.OK);
    }

    public async Task<BaseResponse<string>> AddImageAsync(Guid trainerId, TrainerImageUploadPhotoDto dto)
    {
        if (dto?.File == null || dto.File.Length == 0)
            return new("Image is required", HttpStatusCode.BadRequest);

        var trainer = await _ctx.Trainers
            .Include(t => t.Images)
            .FirstOrDefaultAsync(t => t.Id == trainerId && !t.IsDeleted);

        if (trainer is null) return new("Trainer not found", HttpStatusCode.NotFound);

        // icazə: sahibi və ya policy
        var owner = await IsOwnerAsync(trainerId);
        var canModerate = await CanAsync("Trainers.Moderate");
        if (!(owner || canModerate)) return new("Forbidden", HttpStatusCode.Forbidden);

        // faylı yüklə (storage-a), URL geri qayıdır
        var url = await _files.UploadAsync(dto.File); // məsələn: /uploads/trainers/xyz.jpg

        var img = new Image
        {
            Id = Guid.NewGuid(),
            ImageUrl = url,
            TrainerId = trainerId,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        trainer.Images ??= new List<Image>();
        trainer.Images.Add(img);

        _ctx.Trainers.Update(trainer);
        await _ctx.SaveChangesAsync();

        // Data sahəsində URL-i qaytarırıq
        return new("Image added to trainer", url, HttpStatusCode.Created);
    }

    public async Task<BaseResponse<string>> DeleteImageAsync(Guid trainerId, Guid imageId)
    {
        var trainer = await _ctx.Trainers
            .Include(s => s.Images)
            .FirstOrDefaultAsync(s => s.Id == trainerId && !s.IsDeleted);

        if (trainer is null)
            return new BaseResponse<string>("Trainer not found", HttpStatusCode.NotFound);

        var image = trainer.Images?.FirstOrDefault(i => i.Id == imageId && !i.IsDeleted);
        if (image is null)
            return new BaseResponse<string>("Image not found", HttpStatusCode.NotFound);

        image.IsDeleted = true;
        image.UpdatedAt = DateTime.UtcNow;

        _ctx.Images.Update(image); // DbSet<Image> olmalıdır
        await _ctx.SaveChangesAsync();

        return new BaseResponse<string>("Image deleted from supplement", HttpStatusCode.OK);
    }
    public async Task<BaseResponse<string>> VerifyAsync(Guid id)
    {
        if (!await CanAsync("Trainers.Moderate")) return new("Forbidden", HttpStatusCode.Forbidden);

        var t = await _ctx.Trainers.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (t is null) return new("Trainer not found", HttpStatusCode.NotFound);

        t.IsVerified = true;
        t.UpdatedAt = DateTime.UtcNow;
        await _ctx.SaveChangesAsync();
        return new("Trainer verified", HttpStatusCode.OK);
    }

    public async Task<BaseResponse<string>> ToggleActiveAsync(Guid id, bool isActive)
    {
        if (!await CanAsync("Trainers.Moderate")) return new("Forbidden", HttpStatusCode.Forbidden);

        var t = await _ctx.Trainers.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (t is null) return new("Trainer not found", HttpStatusCode.NotFound);

        t.IsActive = isActive;
        t.UpdatedAt = DateTime.UtcNow;
        await _ctx.SaveChangesAsync();
        return new(isActive ? "Activated" : "Deactivated", HttpStatusCode.OK);
    }

    public async Task<BaseResponse<TrainerGetDto>> GetByIdAsync(Guid id)
    {
        var t = await _ctx.Trainers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (t is null) return new("Trainer not found", null, HttpStatusCode.NotFound);

        var dto = new TrainerGetDto
        {
            Id = t.Id,
            FullName = $"{t.FirstName} {t.LastName}".Trim(),
            Rating = t.Rating,
            TotalRatings = t.TotalRatings,
            IsVerified = t.IsVerified,
            IsActive = t.IsActive,
            Bio = t.Bio,
            Specializations = t.Specializations ?? Array.Empty<string>(),
            Certifications = t.Certifications ?? Array.Empty<string>(),
            ExperienceYears = t.ExperienceYears,
            InstagramUrl = t.InstagramUrl,
            YoutubeUrl = t.YoutubeUrl
        };
        return new("Trainer retrieved", dto, HttpStatusCode.OK);
    }

    public async Task<BaseResponse<List<TrainerListItemDto>>> GetAllAsync(TrainerFilterDto f)
    {
        var q = _ctx.Trainers.AsNoTracking().Where(x => !x.IsDeleted);
        if (f.OnlyActive) q = q.Where(x => x.IsActive);
        if (f.IsVerified.HasValue) q = q.Where(x => x.IsVerified == f.IsVerified.Value);

        if (!string.IsNullOrWhiteSpace(f.Search))
        {
            var s = f.Search.Trim().ToLower();
            q = q.Where(x =>
                (x.FirstName + " " + x.LastName).ToLower().Contains(s) ||
                (x.Bio ?? "").ToLower().Contains(s));
        }

        if (f.Specializations is { Length: > 0 })
            q = q.Where(x => x.Specializations.Any(sp => f.Specializations.Contains(sp)));

        if (f.MinExperienceYears.HasValue)
            q = q.Where(x => x.ExperienceYears >= f.MinExperienceYears.Value);

        q = f.SortBy?.ToLower() switch
        {
            "experience" => (f.Desc ? q.OrderByDescending(x => x.ExperienceYears) : q.OrderBy(x => x.ExperienceYears)),
            "name" => (f.Desc ? q.OrderByDescending(x => x.FirstName).ThenByDescending(x => x.LastName)
                                    : q.OrderBy(x => x.FirstName).ThenBy(x => x.LastName)),
            _ => (f.Desc ? q.OrderByDescending(x => x.Rating).ThenByDescending(x => x.TotalRatings)
                                    : q.OrderBy(x => x.Rating).ThenBy(x => x.TotalRatings))
        };

        var page = Math.Max(1, f.Page);
        var size = Math.Clamp(f.PageSize, 1, 100);

        var list = await q.Skip((page - 1) * size).Take(size)
            .Select(x => new TrainerListItemDto
            {
                Id = x.Id,
                FullName = (x.FirstName + " " + x.LastName).Trim(),
                Rating = x.Rating,
                TotalRatings = x.TotalRatings,
                IsVerified = x.IsVerified,
                IsActive = x.IsActive
            })
            .ToListAsync();

        if (list.Count == 0) return new("No trainers", null, HttpStatusCode.NotFound);
        return new("Trainers retrieved", list, HttpStatusCode.OK);
    }

    public async Task<BaseResponse<List<TrainerListItemDto>>> GetTopAsync(int top = 10)
    {
        top = Math.Clamp(top, 1, 50);
        var list = await _ctx.Trainers.AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsActive)
            .OrderByDescending(x => x.Rating).ThenByDescending(x => x.TotalRatings)
            .Take(top)
            .Select(x => new TrainerListItemDto
            {
                Id = x.Id,
                FullName = (x.FirstName + " " + x.LastName).Trim(),
                Rating = x.Rating,
                TotalRatings = x.TotalRatings,
                IsVerified = x.IsVerified,
                IsActive = x.IsActive
            })
            .ToListAsync();

        if (list.Count == 0) return new("No trainers", null, HttpStatusCode.NotFound);
        return new("Top trainers", list, HttpStatusCode.OK);
    }

    // ================== VIDEOS ==================
    public async Task<BaseResponse<Guid>> CreateVideoAsync(TrainerVideoCreateDto dto)
    {
        var tr = await _ctx.Trainers.FirstOrDefaultAsync(x => x.Id == dto.TrainerId && !x.IsDeleted);
        if (tr is null) return new("Trainer not found", Guid.Empty, HttpStatusCode.BadRequest);

        var owner = await IsOwnerAsync(dto.TrainerId);
        var canModerate = await CanAsync("Trainers.Moderate");
        if (!(owner || canModerate)) return new("Forbidden", HttpStatusCode.Forbidden);

        var id = Guid.NewGuid();
        var v = new TrainerVideo
        {
            Id = id,
            TrainerId = dto.TrainerId,
            Title = dto.Title.Trim(),
            Description = dto.Description?.Trim() ?? "",
            VideoUrl = dto.VideoUrl?.Trim() ?? "",
            ThumbnailUrl = dto.ThumbnailUrl?.Trim(),
            Duration = dto.Duration,
            Type = dto.Type,
            Category = dto.Category,
            IsPremium = dto.IsPremium,
            ViewCount = 0,
            LikeCount = 0,
            PublishedAt = dto.PublishedAt ?? DateTime.UtcNow
        };

        await _ctx.TrainerVideos.AddAsync(v);
        await _ctx.SaveChangesAsync();
        return new("Video created", id, HttpStatusCode.Created);
    }

    public async Task<BaseResponse<List<TrainerVideoListItemDto>>> GetVideosByNameAsync(string name)
    {
        var s = name.Trim().ToLower();
        var list = await _ctx.TrainerVideos.AsNoTracking()
            .Where(v => !v.IsDeleted && v.Title.ToLower().Contains(s))
            .Select(v => new TrainerVideoListItemDto
            {
                Id = v.Id,
                Title = v.Title,
                ThumbnailUrl = v.ThumbnailUrl,
                Duration = v.Duration,
                IsPremium = v.IsPremium,
                ViewCount = v.ViewCount,
                LikeCount = v.LikeCount,
                PublishedAt = v.PublishedAt
            })
            .ToListAsync();

        if (list.Count == 0) return new("No videos found", null, HttpStatusCode.NotFound);
        return new("Videos found", list, HttpStatusCode.OK);
    }

    public async Task<BaseResponse<string>> UpdateVideoAsync(TrainerVideoUpdateDto dto)
    {
        var v = await _ctx.TrainerVideos.FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted);
        if (v is null) return new("Video not found", HttpStatusCode.NotFound);

        var owner = await IsOwnerAsync(v.TrainerId);
        var canModerate = await CanAsync("Trainers.Moderate");
        if (!(owner || canModerate)) return new("Forbidden", HttpStatusCode.Forbidden);

        v.Title = dto.Title.Trim();
        v.Description = dto.Description?.Trim() ?? "";
        v.VideoUrl = dto.VideoUrl?.Trim() ?? v.VideoUrl;
        v.ThumbnailUrl = dto.ThumbnailUrl?.Trim() ?? v.ThumbnailUrl;
        v.Duration = dto.Duration;
        v.Type = dto.Type;
        v.Category = dto.Category;
        v.IsPremium = dto.IsPremium;
        v.PublishedAt = dto.PublishedAt ?? v.PublishedAt;
        v.UpdatedAt = DateTime.UtcNow;

        _ctx.TrainerVideos.Update(v);
        await _ctx.SaveChangesAsync();
        return new("Video updated", HttpStatusCode.OK);
    }

    public async Task<BaseResponse<string>> DeleteVideoAsync(Guid videoId)
    {
        var v = await _ctx.TrainerVideos.FirstOrDefaultAsync(x => x.Id == videoId && !x.IsDeleted);
        if (v is null) return new("Video not found", HttpStatusCode.NotFound);

        var owner = await IsOwnerAsync(v.TrainerId);
        var canModerate = await CanAsync("Trainers.Moderate");
        if (!(owner || canModerate)) return new("Forbidden", HttpStatusCode.Forbidden);

        v.IsDeleted = true;
        v.UpdatedAt = DateTime.UtcNow;
        await _ctx.SaveChangesAsync();
        return new("Video deleted", HttpStatusCode.OK);
    }

    public async Task<BaseResponse<string>> UploadVideoFileAsync(Guid videoId, IFormFile file)
    {
        var v = await _ctx.TrainerVideos.FirstOrDefaultAsync(x => x.Id == videoId && !x.IsDeleted);
        if (v is null) return new("Video not found", HttpStatusCode.NotFound);

        var owner = await IsOwnerAsync(v.TrainerId);
        var canModerate = await CanAsync("Trainers.Moderate");
        if (!(owner || canModerate)) return new("Forbidden", HttpStatusCode.Forbidden);

        if (file == null || file.Length == 0) return new("File required", HttpStatusCode.BadRequest);
        var url = await _files.UploadAsync(file);
        v.VideoUrl = url;
        v.UpdatedAt = DateTime.UtcNow;
        await _ctx.SaveChangesAsync();
        return new("Video file uploaded", HttpStatusCode.OK);
    }

    public async Task<BaseResponse<string>> UploadVideoThumbnailAsync(Guid videoId, IFormFile file)
    {
        var v = await _ctx.TrainerVideos.FirstOrDefaultAsync(x => x.Id == videoId && !x.IsDeleted);
        if (v is null) return new("Video not found", HttpStatusCode.NotFound);

        var owner = await IsOwnerAsync(v.TrainerId);
        var canModerate = await CanAsync("Trainers.Moderate");
        if (!(owner || canModerate)) return new("Forbidden", HttpStatusCode.Forbidden);

        if (file == null || file.Length == 0) return new("File required", HttpStatusCode.BadRequest);
        var url = await _files.UploadAsync(file);
        v.ThumbnailUrl = url;
        v.UpdatedAt = DateTime.UtcNow;
        await _ctx.SaveChangesAsync();
        return new("Thumbnail uploaded", HttpStatusCode.OK);
    }

    public async Task<BaseResponse<TrainerVideoGetDto>> GetVideoByIdAsync(Guid videoId)
    {
        var v = await _ctx.TrainerVideos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == videoId && !x.IsDeleted);
        if (v is null) return new("Video not found", null, HttpStatusCode.NotFound);

        var dto = new TrainerVideoGetDto
        {
            Id = v.Id,
            Title = v.Title,
            Description = v.Description,
            VideoUrl = v.VideoUrl,
            ThumbnailUrl = v.ThumbnailUrl,
            Duration = v.Duration,
            Type = v.Type,
            Category = v.Category,
            IsPremium = v.IsPremium,
            ViewCount = v.ViewCount,
            LikeCount = v.LikeCount,
            PublishedAt = v.PublishedAt
        };
        return new("Video retrieved", dto, HttpStatusCode.OK);
    }

    public async Task<BaseResponse<List<TrainerVideoListItemDto>>> GetTrainerVideosAsync(Guid trainerId, int page = 1, int pageSize = 20)
    {
        var q = _ctx.TrainerVideos.AsNoTracking()
            .Where(x => !x.IsDeleted && x.TrainerId == trainerId)
            .OrderByDescending(x => x.PublishedAt);

        var pageI = Math.Max(1, page);
        var size = Math.Clamp(pageSize, 1, 100);

        var list = await q.Skip((pageI - 1) * size).Take(size)
            .Select(v => new TrainerVideoListItemDto
            {
                Id = v.Id,
                Title = v.Title,
                ThumbnailUrl = v.ThumbnailUrl,
                Duration = v.Duration,
                IsPremium = v.IsPremium,
                ViewCount = v.ViewCount,
                LikeCount = v.LikeCount,
                PublishedAt = v.PublishedAt
            })
            .ToListAsync();

        if (list.Count == 0) return new("No videos", null, HttpStatusCode.NotFound);
        return new("Videos retrieved", list, HttpStatusCode.OK);
    }

    public async Task<BaseResponse<List<TrainerVideoListItemDto>>> GetVideoFeedAsync(TrainerVideoFeedFilterDto f)
    {
        var q = _ctx.TrainerVideos.AsNoTracking().Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(f.Search))
        {
            var s = f.Search.Trim().ToLower();
            q = q.Where(x => x.Title.ToLower().Contains(s) || (x.Description ?? "").ToLower().Contains(s));
        }

        if (f.Category.HasValue) q = q.Where(x => x.Category == f.Category);
        if (f.Type.HasValue) q = q.Where(x => x.Type == f.Type);
        if (f.IsPremium.HasValue) q = q.Where(x => x.IsPremium == f.IsPremium.Value);

        q = f.SortBy?.ToLower() switch
        {
            "views" => (f.Desc ? q.OrderByDescending(x => x.ViewCount) : q.OrderBy(x => x.ViewCount)),
            "likes" => (f.Desc ? q.OrderByDescending(x => x.LikeCount) : q.OrderBy(x => x.LikeCount)),
            _ => (f.Desc ? q.OrderByDescending(x => x.PublishedAt) : q.OrderBy(x => x.PublishedAt))
        };

        var page = Math.Max(1, f.Page);
        var size = Math.Clamp(f.PageSize, 1, 100);

        var list = await q.Skip((page - 1) * size).Take(size)
            .Select(v => new TrainerVideoListItemDto
            {
                Id = v.Id,
                Title = v.Title,
                ThumbnailUrl = v.ThumbnailUrl,
                Duration = v.Duration,
                IsPremium = v.IsPremium,
                ViewCount = v.ViewCount,
                LikeCount = v.LikeCount,
                PublishedAt = v.PublishedAt
            })
            .ToListAsync();

        if (list.Count == 0) return new("No videos", null, HttpStatusCode.NotFound);
        return new("Videos retrieved", list, HttpStatusCode.OK);
    }



 
    public async Task<BaseResponse<List<TrainerVideoListItemDto>>> GetRecentVideosAsync(
    int days = 30, int page = 1, int pageSize = 20)
    {
        var from = DateTime.UtcNow.AddDays(-Math.Max(1, days));

        var q = _ctx.TrainerVideos.AsNoTracking()
            .Where(v => !v.IsDeleted && v.PublishedAt >= from)
            .OrderByDescending(v => v.PublishedAt);

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var list = await q.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(v => new TrainerVideoListItemDto
            {
                Id = v.Id,
                Title = v.Title,
                ThumbnailUrl = v.ThumbnailUrl,
                Duration = v.Duration,
                IsPremium = v.IsPremium,
                ViewCount = v.ViewCount,
                LikeCount = v.LikeCount,
                PublishedAt = v.PublishedAt
            }).ToListAsync();

        if (list.Count == 0) return new("No recent videos", null, HttpStatusCode.NotFound);
        return new("Recent videos", list, HttpStatusCode.OK);
    }

    public async Task<BaseResponse<List<TrainerVideoListItemDto>>> GetPopularVideosAsync(
        int page = 1, int pageSize = 20)
    {
        // Popularlığı sadə “score” ilə ölçürük (fav/review yoxdursa belə işləyir)
        // score = ViewCount*2 + LikeCount
        var q = _ctx.TrainerVideos.AsNoTracking()
            .Where(v => !v.IsDeleted)
            .OrderByDescending(v => (long)v.ViewCount * 2 + v.LikeCount)
            .ThenByDescending(v => v.PublishedAt);

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var list = await q.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(v => new TrainerVideoListItemDto
            {
                Id = v.Id,
                Title = v.Title,
                ThumbnailUrl = v.ThumbnailUrl,
                Duration = v.Duration,
                IsPremium = v.IsPremium,
                ViewCount = v.ViewCount,
                LikeCount = v.LikeCount,
                PublishedAt = v.PublishedAt
            }).ToListAsync();

        if (list.Count == 0) return new("No popular videos", null, HttpStatusCode.NotFound);
        return new("Popular videos", list, HttpStatusCode.OK);
    }

    // ================== WORKOUTS ==================
    public async Task<BaseResponse<Guid>> CreateWorkoutAsync(TrainerWorkoutCreateDto dto)
    {
        var tr = await _ctx.Trainers.FirstOrDefaultAsync(x => x.Id == dto.TrainerId && !x.IsDeleted);
        if (tr is null) return new("Trainer not found", Guid.Empty, HttpStatusCode.BadRequest);

        var owner = await IsOwnerAsync(dto.TrainerId);
        var canModerate = await CanAsync("Trainers.Moderate");
        if (!(owner || canModerate)) return new("Forbidden", HttpStatusCode.Forbidden);

        var exIds = dto.Lines.Select(l => l.ExerciseId).ToHashSet();
        var found = await _ctx.Exercises
            .Where(e => !e.IsDeleted && exIds.Contains(e.Id))
            .Select(e => e.Id)
            .ToListAsync();
        if (exIds.Except(found).Any()) return new("Some exercises not found", Guid.Empty, HttpStatusCode.BadRequest);

        var id = Guid.NewGuid();
        var w = new TrainerWorkout
        {
            Id = id,
            TrainerId = dto.TrainerId,
            Title = dto.Title.Trim(),
            Description = dto.Description?.Trim() ?? "",
            Category = dto.Category,
            Difficulty = dto.Difficulty,
            EstimatedDuration = dto.EstimatedDuration,
            TargetMuscles = dto.TargetMuscles,
            RequiredEquipment = dto.RequiredEquipment,
            ThumbnailUrl = dto.ThumbnailUrl?.Trim(),
            PreviewVideoUrl = dto.PreviewVideoUrl?.Trim(),
            IsPremium = dto.IsPremium,
            ViewCount = 0,
            LikeCount = 0,
            PublishedAt = dto.PublishedAt ?? DateTime.UtcNow,
            WorkoutExercises = dto.Lines.Select(l => new TrainerWorkoutExercise
            {
                Id = Guid.NewGuid(),
                ExerciseId = l.ExerciseId,
                Sets = l.Sets,
                Reps = l.Reps,
                Duration = l.Duration,
                RestTimeSeconds = l.RestTimeSeconds,
                TrainerNotes = l.TrainerNotes,
                VideoUrl = l.VideoUrl
            }).ToList()
        };

        await _ctx.TrainerWorkouts.AddAsync(w);
        await _ctx.SaveChangesAsync();
        return new("Trainer workout created", id, HttpStatusCode.Created);
    }

    public async Task<BaseResponse<List<TrainerWorkoutListItemDto>>> GetWorkoutsByNameAsync(string name)
    {
        var s = name.Trim().ToLower();
        var list = await _ctx.TrainerWorkouts.AsNoTracking()
            .Where(w => !w.IsDeleted && w.Title.ToLower().Contains(s))
            .Select(w => new TrainerWorkoutListItemDto
            {
                Id = w.Id,
                Title = w.Title,
                ThumbnailUrl = w.ThumbnailUrl,
                IsPremium = w.IsPremium,
                ViewCount = w.ViewCount,
                LikeCount = w.LikeCount,
                PublishedAt = w.PublishedAt,
                Difficulty = w.Difficulty,
                Category = w.Category
            })
            .ToListAsync();

        if (list.Count == 0) return new("No workouts found", null, HttpStatusCode.NotFound);
        return new("Workouts found", list, HttpStatusCode.OK);
    }
    public async Task<BaseResponse<List<TrainerWorkoutListItemDto>>> GetWorkoutsByDifficultyAsync(
    DifficultyLevel level, int page = 1, int pageSize = 20)
    {
        var q = _ctx.TrainerWorkouts.AsNoTracking()
            .Where(w => !w.IsDeleted && w.Difficulty == level)
            .OrderByDescending(w => w.PublishedAt);

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var list = await q.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(w => new TrainerWorkoutListItemDto
            {
                Id = w.Id,
                Title = w.Title,
                ThumbnailUrl = w.ThumbnailUrl,
                IsPremium = w.IsPremium,
                ViewCount = w.ViewCount,
                LikeCount = w.LikeCount,
                PublishedAt = w.PublishedAt,
                Difficulty = w.Difficulty,
                Category = w.Category
            }).ToListAsync();

        if (list.Count == 0) return new("No workouts", null, HttpStatusCode.NotFound);
        return new("Workouts by difficulty", list, HttpStatusCode.OK);
    }

    public async Task<BaseResponse<List<TrainerWorkoutListItemDto>>> GetWorkoutsByCategoryAsync(
        WorkoutCategory category, int page = 1, int pageSize = 20)
    {
        var q = _ctx.TrainerWorkouts.AsNoTracking()
            .Where(w => !w.IsDeleted && w.Category == category)
            .OrderByDescending(w => w.PublishedAt);

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var list = await q.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(w => new TrainerWorkoutListItemDto
            {
                Id = w.Id,
                Title = w.Title,
                ThumbnailUrl = w.ThumbnailUrl,
                IsPremium = w.IsPremium,
                ViewCount = w.ViewCount,
                LikeCount = w.LikeCount,
                PublishedAt = w.PublishedAt,
                Difficulty = w.Difficulty,
                Category = w.Category
            }).ToListAsync();

        if (list.Count == 0) return new("No workouts", null, HttpStatusCode.NotFound);
        return new("Workouts by category", list, HttpStatusCode.OK);
    }

    public async Task<BaseResponse<string>> UpdateWorkoutAsync(TrainerWorkoutUpdateDto dto)
    {
        var w = await _ctx.TrainerWorkouts
            .Include(x => x.WorkoutExercises)
            .FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted);
        if (w is null) return new("Workout not found", HttpStatusCode.NotFound);

        var owner = await IsOwnerAsync(w.TrainerId);
        var canModerate = await CanAsync("Trainers.Moderate");
        if (!(owner || canModerate)) return new("Forbidden", HttpStatusCode.Forbidden);

        var exIds = dto.Lines.Select(l => l.ExerciseId).ToHashSet();
        var found = await _ctx.Exercises
            .Where(e => !e.IsDeleted && exIds.Contains(e.Id))
            .Select(e => e.Id)
            .ToListAsync();
        if (exIds.Except(found).Any()) return new("Some exercises not found", HttpStatusCode.BadRequest);

        w.Title = dto.Title.Trim();
        w.Description = dto.Description?.Trim() ?? "";
        w.Category = dto.Category;
        w.Difficulty = dto.Difficulty;
        w.EstimatedDuration = dto.EstimatedDuration;
        w.TargetMuscles = dto.TargetMuscles;
        w.RequiredEquipment = dto.RequiredEquipment;
        w.ThumbnailUrl = dto.ThumbnailUrl?.Trim();
        w.PreviewVideoUrl = dto.PreviewVideoUrl?.Trim();
        w.IsPremium = dto.IsPremium;
        w.PublishedAt = dto.PublishedAt ?? w.PublishedAt;
        w.UpdatedAt = DateTime.UtcNow;

        _ctx.TrainerWorkoutExercises.RemoveRange(w.WorkoutExercises);
        w.WorkoutExercises = dto.Lines.Select(l => new TrainerWorkoutExercise
        {
            Id = Guid.NewGuid(),
            TrainerWorkoutId = w.Id,
            ExerciseId = l.ExerciseId,
            Sets = l.Sets,
            Reps = l.Reps,
            Duration = l.Duration,
            RestTimeSeconds = l.RestTimeSeconds,
            TrainerNotes = l.TrainerNotes,
            VideoUrl = l.VideoUrl
        }).ToList();

        await _ctx.SaveChangesAsync();
        return new("Workout updated", HttpStatusCode.OK);
    }

    public async Task<BaseResponse<string>> DeleteWorkoutAsync(Guid workoutId)
    {
        var w = await _ctx.TrainerWorkouts.FirstOrDefaultAsync(x => x.Id == workoutId && !x.IsDeleted);
        if (w is null) return new("Workout not found", HttpStatusCode.NotFound);

        var owner = await IsOwnerAsync(w.TrainerId);
        var canModerate = await CanAsync("Trainers.Moderate");
        if (!(owner || canModerate)) return new("Forbidden", HttpStatusCode.Forbidden);

        w.IsDeleted = true;
        w.UpdatedAt = DateTime.UtcNow;
        await _ctx.SaveChangesAsync();
        return new("Workout deleted", HttpStatusCode.OK);
    }

    public async Task<BaseResponse<string>> UploadWorkoutThumbnailAsync(Guid workoutId, IFormFile file)
    {
        var w = await _ctx.TrainerWorkouts.FirstOrDefaultAsync(x => x.Id == workoutId && !x.IsDeleted);
        if (w is null) return new("Workout not found", HttpStatusCode.NotFound);

        var owner = await IsOwnerAsync(w.TrainerId);
        var canModerate = await CanAsync("Trainers.Moderate");
        if (!(owner || canModerate)) return new("Forbidden", HttpStatusCode.Forbidden);

        if (file == null || file.Length == 0) return new("File required", HttpStatusCode.BadRequest);
        var url = await _files.UploadAsync(file);
        w.ThumbnailUrl = url;
        w.UpdatedAt = DateTime.UtcNow;
        await _ctx.SaveChangesAsync();
        return new("Thumbnail uploaded", HttpStatusCode.OK);
    }

    public async Task<BaseResponse<string>> UploadWorkoutPreviewAsync(Guid workoutId, IFormFile file)
    {
        var w = await _ctx.TrainerWorkouts.FirstOrDefaultAsync(x => x.Id == workoutId && !x.IsDeleted);
        if (w is null) return new("Workout not found", HttpStatusCode.NotFound);

        var owner = await IsOwnerAsync(w.TrainerId);
        var canModerate = await CanAsync("Trainers.Moderate");
        if (!(owner || canModerate)) return new("Forbidden", HttpStatusCode.Forbidden);

        if (file == null || file.Length == 0) return new("File required", HttpStatusCode.BadRequest);
        var url = await _files.UploadAsync(file);
        w.PreviewVideoUrl = url;
        w.UpdatedAt = DateTime.UtcNow;
        await _ctx.SaveChangesAsync();
        return new("Preview uploaded", HttpStatusCode.OK);
    }

    public async Task<BaseResponse<TrainerWorkoutGetDto>> GetWorkoutByIdAsync(Guid workoutId)
    {
        var w = await _ctx.TrainerWorkouts
            .Include(x => x.WorkoutExercises).ThenInclude(le => le.Exercise)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == workoutId && !x.IsDeleted);

        if (w is null) return new("Workout not found", null, HttpStatusCode.NotFound);

        var dto = new TrainerWorkoutGetDto
        {
            Id = w.Id,
            Title = w.Title,
            Description = w.Description,
            Category = w.Category,
            Difficulty = w.Difficulty,
            EstimatedDuration = w.EstimatedDuration,
            TargetMuscles = w.TargetMuscles,
            RequiredEquipment = w.RequiredEquipment,
            ThumbnailUrl = w.ThumbnailUrl,
            PreviewVideoUrl = w.PreviewVideoUrl,
            IsPremium = w.IsPremium,
            ViewCount = w.ViewCount,
            LikeCount = w.LikeCount,
            PublishedAt = w.PublishedAt,
            Lines = w.WorkoutExercises.Select(l => new TrainerWorkoutLineDto
            {
                ExerciseId = l.ExerciseId,
                Sets = l.Sets,
                Reps = l.Reps,
                Duration = l.Duration,
                RestTimeSeconds = l.RestTimeSeconds,
                TrainerNotes = l.TrainerNotes,
                VideoUrl = l.VideoUrl
            }).ToList()
        };
        return new("Workout retrieved", dto, HttpStatusCode.OK);
    }

    public async Task<BaseResponse<List<TrainerWorkoutListItemDto>>> GetTrainerWorkoutsAsync(Guid trainerId, int page = 1, int pageSize = 20)
    {
        var q = _ctx.TrainerWorkouts.AsNoTracking()
            .Where(x => !x.IsDeleted && x.TrainerId == trainerId)
            .OrderByDescending(x => x.PublishedAt);

        var pageI = Math.Max(1, page);
        var size = Math.Clamp(pageSize, 1, 100);

        var list = await q.Skip((pageI - 1) * size).Take(size)
            .Select(w => new TrainerWorkoutListItemDto
            {
                Id = w.Id,
                Title = w.Title,
                ThumbnailUrl = w.ThumbnailUrl,
                IsPremium = w.IsPremium,
                ViewCount = w.ViewCount,
                LikeCount = w.LikeCount,
                PublishedAt = w.PublishedAt,
                Difficulty = w.Difficulty,
                Category = w.Category
            })
            .ToListAsync();

        if (list.Count == 0) return new("No workouts", null, HttpStatusCode.NotFound);
        return new("Workouts retrieved", list, HttpStatusCode.OK);
    }

    public async Task<BaseResponse<List<TrainerWorkoutListItemDto>>> GetWorkoutFeedAsync(TrainerWorkoutFilterDto f)
    {
        var q = _ctx.TrainerWorkouts.AsNoTracking().Where(x => !x.IsDeleted);

        if (f.TrainerId.HasValue) q = q.Where(x => x.TrainerId == f.TrainerId.Value);
        if (f.Category.HasValue) q = q.Where(x => x.Category == f.Category.Value);
        if (f.Difficulty.HasValue) q = q.Where(x => x.Difficulty == f.Difficulty.Value);
        if (f.IsPremium.HasValue) q = q.Where(x => x.IsPremium == f.IsPremium.Value);

        q = f.SortBy?.ToLower() switch
        {
            "views" => (f.Desc ? q.OrderByDescending(x => x.ViewCount) : q.OrderBy(x => x.ViewCount)),
            "likes" => (f.Desc ? q.OrderByDescending(x => x.LikeCount) : q.OrderBy(x => x.LikeCount)),
            _ => (f.Desc ? q.OrderByDescending(x => x.PublishedAt) : q.OrderBy(x => x.PublishedAt))
        };

        var page = Math.Max(1, f.Page);
        var size = Math.Clamp(f.PageSize, 1, 100);

        var list = await q.Skip((page - 1) * size).Take(size)
            .Select(w => new TrainerWorkoutListItemDto
            {
                Id = w.Id,
                Title = w.Title,
                ThumbnailUrl = w.ThumbnailUrl,
                IsPremium = w.IsPremium,
                ViewCount = w.ViewCount,
                LikeCount = w.LikeCount,
                PublishedAt = w.PublishedAt,
                Difficulty = w.Difficulty,
                Category = w.Category
            })
            .ToListAsync();

        if (list.Count == 0) return new("No workouts", null, HttpStatusCode.NotFound);
        return new("Workouts retrieved", list, HttpStatusCode.OK);
    }

 



}

