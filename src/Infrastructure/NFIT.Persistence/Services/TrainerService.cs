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
    private readonly IHttpContextAccessor _http;
    private readonly ICloudinaryService _cloud;

    public TrainerService(NFITDbContext ctx, IFileService files, IHttpContextAccessor http, ICloudinaryService cloud)
    {
        _ctx = ctx;
        _http = http;
        _cloud = cloud;
    }

    // ================== AUTH / HELPERS ==================
    private ClaimsPrincipal? User => _http.HttpContext?.User;

    private string? CurrentUserId =>
        User?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User?.FindFirst("sub")?.Value;

    private bool HasPermission(string permission)
        => User?.HasClaim("permission", permission) == true;

    private async Task<bool> IsOwnerAsync(Guid trainerId)
        => await _ctx.Trainers
            .Where(t => t.Id == trainerId && !t.IsDeleted)
            .Select(t => t.UserId == CurrentUserId)
            .FirstOrDefaultAsync();

    private async Task<bool> OwnerOrPermissionAsync(Guid trainerId, string permission)
        => (await IsOwnerAsync(trainerId)) || HasPermission(permission);

    // ================== TRAINER ==================
    public async Task<BaseResponse<Guid>> CreateAsync(TrainerCreateDto dto)
    {
        var userId = dto.UserId ?? CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
            return new("Unauthorized", Guid.Empty, HttpStatusCode.Unauthorized);

        // Başqasının adından yaratmaq üçün permission tələb et
        if (userId != CurrentUserId && !HasPermission(Permissions.Trainer.Create))
            return new("Forbidden", Guid.Empty, HttpStatusCode.Forbidden);

        // Eyni user üçün ikinci trainerin qarşısını al (opsional, tövsiyə olunur)
        var already = await _ctx.Trainers.AnyAsync(t => t.UserId == userId && !t.IsDeleted);
        if (already) return new("Trainer already exists for this user", Guid.Empty, HttpStatusCode.Conflict);

        if (string.IsNullOrWhiteSpace(dto.FirstName) || string.IsNullOrWhiteSpace(dto.LastName))
            return new("FirstName and LastName are required", Guid.Empty, HttpStatusCode.BadRequest);

        dto.FirstName = dto.FirstName.Trim();
        dto.LastName = dto.LastName.Trim();
        if (dto.FirstName.Length < 2 || dto.FirstName.Length > 60 ||
            dto.LastName.Length < 2 || dto.LastName.Length > 60)
            return new("Name parts must be 2-60 characters", Guid.Empty, HttpStatusCode.BadRequest);

        if (dto.ExperienceYears < 0 || dto.ExperienceYears > 80)
            return new("ExperienceYears must be between 0 and 80", Guid.Empty, HttpStatusCode.BadRequest);

        var id = Guid.NewGuid();
        var t = new Trainer
        {
            Id = id,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
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

        var allowed = await OwnerOrPermissionAsync(dto.Id, Permissions.Trainer.Update);
        if (!allowed) return new("Forbidden", HttpStatusCode.Forbidden);

        if (string.IsNullOrWhiteSpace(dto.FirstName) || string.IsNullOrWhiteSpace(dto.LastName))
            return new("FirstName and LastName are required", HttpStatusCode.BadRequest);

        dto.FirstName = dto.FirstName.Trim();
        dto.LastName = dto.LastName.Trim();
        if (dto.FirstName.Length < 2 || dto.FirstName.Length > 60 ||
            dto.LastName.Length < 2 || dto.LastName.Length > 60)
            return new("Name parts must be 2-60 characters", HttpStatusCode.BadRequest);

        if (dto.ExperienceYears < 0 || dto.ExperienceYears > 80)
            return new("ExperienceYears must be between 0 and 80", HttpStatusCode.BadRequest);

        t.FirstName = dto.FirstName;
        t.LastName = dto.LastName;
        t.Bio = dto.Bio?.Trim() ?? "";
        t.Specializations = dto.Specializations ?? Array.Empty<string>();
        t.Certifications = dto.Certifications ?? Array.Empty<string>();
        t.ExperienceYears = dto.ExperienceYears;
        t.InstagramUrl = dto.InstagramUrl?.Trim();
        t.YoutubeUrl = dto.YoutubeUrl?.Trim();
        t.IsActive = dto.IsActive;

        // Verified ayrıca endpoint ilə edilir
        t.UpdatedAt = DateTime.UtcNow;

        _ctx.Trainers.Update(t);
        await _ctx.SaveChangesAsync();
        return new("Trainer updated", HttpStatusCode.OK);
    }

    public async Task<BaseResponse<string>> DeleteAsync(Guid id)
    {
        var t = await _ctx.Trainers.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (t is null) return new("Trainer not found", HttpStatusCode.NotFound);

        var allowed = await OwnerOrPermissionAsync(id, Permissions.Trainer.Delete);
        if (!allowed) return new("Forbidden", HttpStatusCode.Forbidden);

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

        if (!string.IsNullOrWhiteSpace(dto.File.ContentType) &&
            !dto.File.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return new("Only image files are allowed", HttpStatusCode.BadRequest);

        var trainer = await _ctx.Trainers
            .Include(t => t.Images)
            .FirstOrDefaultAsync(t => t.Id == trainerId && !t.IsDeleted);
        if (trainer is null) return new("Trainer not found", HttpStatusCode.NotFound);

        var allowed = await OwnerOrPermissionAsync(trainerId, Permissions.Trainer.AddImageAsync);
        if (!allowed) return new("Forbidden", HttpStatusCode.Forbidden);

        // Cloudinary folder
        var folder = $"NFIT/trainers/{trainerId}";
        var (url, publicId) = await _cloud.UploadImageAsync(dto.File, folder);
        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(publicId))
            return new("Upload failed", HttpStatusCode.BadRequest);

        var img = new Image
        {
            Id = Guid.NewGuid(),
            ImageUrl = url,
            PublicId = publicId,     // <-- saxla
            TrainerId = trainerId,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        await _ctx.Images.AddAsync(img);
        await _ctx.SaveChangesAsync();

        return new("Image added to trainer", url, HttpStatusCode.Created);
    }

    public async Task<BaseResponse<string>> DeleteImageAsync(Guid trainerId, Guid imageId)
    {
        var trainer = await _ctx.Trainers
            .Include(t => t.Images)
            .FirstOrDefaultAsync(t => t.Id == trainerId && !t.IsDeleted);

        if (trainer is null)
            return new("Trainer not found", HttpStatusCode.NotFound);

        var allowed = await OwnerOrPermissionAsync(trainerId, Permissions.Trainer.DeleteImageAsync);
        if (!allowed) return new("Forbidden", HttpStatusCode.Forbidden);

        var image = trainer.Images?.FirstOrDefault(i => i.Id == imageId);
        if (image is null)
            return new("Image not found", HttpStatusCode.NotFound);

        // 1) Cloudinary-dən sil (yalnız "ok" uğur say)
        var cloudDeleted = true;
        if (!string.IsNullOrWhiteSpace(image.PublicId))
            cloudDeleted = await _cloud.DeleteImageAsync(image.PublicId);
        if (!cloudDeleted)
            return new("Failed to delete from Cloudinary", HttpStatusCode.BadRequest);

        // 2) DB-dən HARD sil
        _ctx.Images.Remove(image);
        await _ctx.SaveChangesAsync();

        return new("Image hard-deleted from trainer", HttpStatusCode.OK);
    }

    public async Task<BaseResponse<string>> VerifyAsync(Guid id)
    {
        if (!HasPermission(Permissions.Trainer.Verify)) return new("Forbidden", HttpStatusCode.Forbidden);

        var t = await _ctx.Trainers.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (t is null) return new("Trainer not found", HttpStatusCode.NotFound);

        t.IsVerified = true;
        t.UpdatedAt = DateTime.UtcNow;
        await _ctx.SaveChangesAsync();
        return new("Trainer verified", HttpStatusCode.OK);
    }

    public async Task<BaseResponse<string>> ToggleActiveAsync(Guid id, bool isActive)
    {
        if (!HasPermission(Permissions.Trainer.ToggleActive)) return new("Forbidden", HttpStatusCode.Forbidden);

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

        var allowed = await OwnerOrPermissionAsync(dto.TrainerId, Permissions.Trainer.CreateVideo);
        if (!allowed) return new("Forbidden", Guid.Empty, HttpStatusCode.Forbidden);

        if (string.IsNullOrWhiteSpace(dto.Title))
            return new("Title is required", Guid.Empty, HttpStatusCode.BadRequest);
        var title = dto.Title.Trim();
        if (title.Length < 2 || title.Length > 150)
            return new("Title length must be 2..150", Guid.Empty, HttpStatusCode.BadRequest);

        if (!Enum.IsDefined(typeof(VideoType), dto.Type))
            return new("Invalid video type", Guid.Empty, HttpStatusCode.BadRequest);

        if (!Enum.IsDefined(typeof(WorkoutCategory), dto.Category))
            return new("Invalid video category", Guid.Empty, HttpStatusCode.BadRequest);

        if (dto.Duration < 0)
            return new("Duration cannot be negative", Guid.Empty, HttpStatusCode.BadRequest);

        var videoUrl = dto.VideoUrl?.Trim() ?? "";
        var thumbUrl = dto.ThumbnailUrl?.Trim();

        var id = Guid.NewGuid();
        var v = new TrainerVideo
        {
            Id = id,
            TrainerId = dto.TrainerId,
            Title = title,
            Description = dto.Description?.Trim() ?? "",
            VideoUrl = videoUrl,
            ThumbnailUrl = thumbUrl,
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

        var allowed = await OwnerOrPermissionAsync(v.TrainerId, Permissions.Trainer.UpdateVideo);
        if (!allowed) return new("Forbidden", HttpStatusCode.Forbidden);

        if (string.IsNullOrWhiteSpace(dto.Title))
            return new("Title is required", HttpStatusCode.BadRequest);
        var title = dto.Title.Trim();
        if (title.Length < 2 || title.Length > 150)
            return new("Title length must be 2..150", HttpStatusCode.BadRequest);

        if (!Enum.IsDefined(typeof(VideoType), dto.Type))
            return new("Invalid video type", HttpStatusCode.BadRequest);

        if (!Enum.IsDefined(typeof(WorkoutCategory), dto.Category))
            return new("Invalid video category", HttpStatusCode.BadRequest);

        if (dto.Duration < 0)
            return new("Duration cannot be negative", HttpStatusCode.BadRequest);

        v.Title = title;
        v.Description = dto.Description?.Trim() ?? "";
        v.VideoUrl = string.IsNullOrWhiteSpace(dto.VideoUrl) ? v.VideoUrl : dto.VideoUrl!.Trim();
        v.ThumbnailUrl = string.IsNullOrWhiteSpace(dto.ThumbnailUrl) ? v.ThumbnailUrl : dto.ThumbnailUrl!.Trim();
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

        var allowed = await OwnerOrPermissionAsync(v.TrainerId, Permissions.Trainer.DeleteVideo);
        if (!allowed) return new("Forbidden", HttpStatusCode.Forbidden);

        v.IsDeleted = true;
        v.UpdatedAt = DateTime.UtcNow;
        await _ctx.SaveChangesAsync();
        return new("Video deleted", HttpStatusCode.OK);
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

    public async Task<BaseResponse<List<TrainerVideoListItemDto>>> GetRecentVideosAsync(int days = 30, int page = 1, int pageSize = 20)
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

    public async Task<BaseResponse<List<TrainerVideoListItemDto>>> GetPopularVideosAsync(int page = 1, int pageSize = 20)
    {
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

        var allowed = await OwnerOrPermissionAsync(dto.TrainerId, Permissions.Trainer.CreateWorkout);
        if (!allowed) return new("Forbidden", Guid.Empty, HttpStatusCode.Forbidden);

        if (string.IsNullOrWhiteSpace(dto.Title))
            return new("Title is required", Guid.Empty, HttpStatusCode.BadRequest);
        var title = dto.Title.Trim();
        if (title.Length < 2 || title.Length > 150)
            return new("Title length must be 2..150", Guid.Empty, HttpStatusCode.BadRequest);

        if (!Enum.IsDefined(typeof(WorkoutCategory), dto.Category))
            return new("Invalid workout category", Guid.Empty, HttpStatusCode.BadRequest);

        if (!Enum.IsDefined(typeof(DifficultyLevel), dto.Difficulty))
            return new("Invalid difficulty", Guid.Empty, HttpStatusCode.BadRequest);

        if (!Enum.IsDefined(typeof(MuscleGroup), dto.TargetMuscles))
            return new("Invalid muscle group", Guid.Empty, HttpStatusCode.BadRequest);

        if (dto.EstimatedDuration < 1)
            return new("EstimatedDuration must be >= 1 minute", Guid.Empty, HttpStatusCode.BadRequest);

        if (dto.Lines == null || dto.Lines.Count == 0)
            return new("At least one exercise line is required", Guid.Empty, HttpStatusCode.BadRequest);

        foreach (var l in dto.Lines)
        {
            if (l.Sets < 0) return new("Line.Sets cannot be negative", Guid.Empty, HttpStatusCode.BadRequest);
            if (l.Reps < 0) return new("Line.Reps cannot be negative", Guid.Empty, HttpStatusCode.BadRequest);
            if (l.Duration < 0) return new("Line.Duration cannot be negative", Guid.Empty, HttpStatusCode.BadRequest);
            if (l.RestTimeSeconds < 0) return new("Line.RestTimeSeconds cannot be negative", Guid.Empty, HttpStatusCode.BadRequest);
        }

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
            Title = title,
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

        var allowed = await OwnerOrPermissionAsync(w.TrainerId, Permissions.Trainer.UpdateWorkout);
        if (!allowed) return new("Forbidden", HttpStatusCode.Forbidden);

        if (string.IsNullOrWhiteSpace(dto.Title))
            return new("Title is required", HttpStatusCode.BadRequest);
        if (dto.Title.Trim().Length is < 2 or > 160)
            return new("Title length must be 2-160", HttpStatusCode.BadRequest);

        if (!Enum.IsDefined(typeof(WorkoutCategory), dto.Category))
            return new("Invalid workout category", HttpStatusCode.BadRequest);

        if (!Enum.IsDefined(typeof(DifficultyLevel), dto.Difficulty))
            return new("Invalid difficulty", HttpStatusCode.BadRequest);

        if (!Enum.IsDefined(typeof(MuscleGroup), dto.TargetMuscles))
            return new("Invalid muscle group", HttpStatusCode.BadRequest);

        if (dto.EstimatedDuration < 0)
            return new("EstimatedDuration cannot be negative", HttpStatusCode.BadRequest);

        if (dto.Lines == null || dto.Lines.Count == 0)
            return new("At least one exercise line is required", HttpStatusCode.BadRequest);

        foreach (var l in dto.Lines)
        {
            if (l.Sets < 0 || l.Reps < 0 || l.Duration < 0 || l.RestTimeSeconds < 0)
                return new("Sets/Reps/Duration/Rest must be >= 0", HttpStatusCode.BadRequest);

            if ((l.Sets == 0 && l.Reps == 0 && l.Duration == 0))
                return new("Each line must have either sets/reps or duration", HttpStatusCode.BadRequest);
        }

        var exIds = dto.Lines.Select(l => l.ExerciseId).ToHashSet();
        var found = await _ctx.Exercises
            .Where(e => !e.IsDeleted && exIds.Contains(e.Id))
            .Select(e => e.Id)
            .ToListAsync();
        if (exIds.Except(found).Any())
            return new("Some exercises not found", HttpStatusCode.BadRequest);

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

        var allowed = await OwnerOrPermissionAsync(w.TrainerId, Permissions.Trainer.DeleteWorkout);
        if (!allowed) return new("Forbidden", HttpStatusCode.Forbidden);

        w.IsDeleted = true;
        w.UpdatedAt = DateTime.UtcNow;
        await _ctx.SaveChangesAsync();
        return new("Workout deleted", HttpStatusCode.OK);
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

