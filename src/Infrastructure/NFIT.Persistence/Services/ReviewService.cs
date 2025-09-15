using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NFIT.Application.Abstracts.Services;
using NFIT.Application.DTOs.ReviewDtos;
using NFIT.Application.Shared;
using NFIT.Domain.Entities;
using NFIT.Persistence.Contexts;

namespace NFIT.Persistence.Services;

public class ReviewService:IReviewService
{
    private readonly NFITDbContext _context;
    private readonly IHttpContextAccessor _http;

    public ReviewService(NFITDbContext context, IHttpContextAccessor http)
    {
        _context = context;
        _http = http;
    }

    private string? CurrentUserId =>
        _http.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? _http.HttpContext?.User?.FindFirst("sub")?.Value;

    private static bool SingleTarget(Guid? gymId, Guid? trainerId, Guid? supplementId)
    {
        var c = 0;
        if (gymId.HasValue) c++;
        if (trainerId.HasValue) c++;
        if (supplementId.HasValue) c++;
        return c == 1;
    }

    public async Task<BaseResponse<Guid>> CreateAsync(ReviewCreateDto dto)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
            return new BaseResponse<Guid>("Unauthorized", Guid.Empty, HttpStatusCode.Unauthorized);

        if (!SingleTarget(dto.GymId, dto.TrainerId, dto.SupplementId))
            return new BaseResponse<Guid>("Exactly one of GymId/TrainerId/SupplementId must be provided", Guid.Empty, HttpStatusCode.BadRequest);

        // Duplicate check: user already reviewed this target?
        var exists = await _context.Reviews.AnyAsync(r =>
            !r.IsDeleted &&
            r.UserId == userId &&
            r.GymID == dto.GymId &&
            r.TrainerId == dto.TrainerId &&
            r.SupplementId == dto.SupplementId);

        if (exists)
            return new BaseResponse<Guid>("You have already reviewed this target", Guid.Empty, HttpStatusCode.Conflict);

        var entity = new Review
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            GymID = dto.GymId,
            TrainerId = dto.TrainerId,
            SupplementId = dto.SupplementId,
            Content = dto.Content?.Trim(),
            Rating = dto.Rating,
            IsApproved = false,           // default: pending
            ApprovedAt = null,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Reviews.AddAsync(entity);
        await _context.SaveChangesAsync();

        return new BaseResponse<Guid>("Review created (pending approval)", entity.Id, HttpStatusCode.Created);
    }

    public async Task<BaseResponse<string>> UpdateAsync(ReviewUpdateDto dto)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
            return new BaseResponse<string>("Unauthorized", HttpStatusCode.Unauthorized);

        var review = await _context.Reviews.FirstOrDefaultAsync(r => r.Id == dto.Id && !r.IsDeleted);
        if (review is null)
            return new BaseResponse<string>("Review not found", HttpStatusCode.NotFound);

        if (review.UserId != userId)
            return new BaseResponse<string>("Forbidden", HttpStatusCode.Forbidden);

        // Update fields
        review.Content = dto.Content?.Trim();
        review.Rating = dto.Rating;

        // Changing review -> requires re-approval
        review.IsApproved = false;
        review.ApprovedAt = null;
        review.UpdatedAt = DateTime.UtcNow;

        _context.Reviews.Update(review);
        await _context.SaveChangesAsync();

        return new BaseResponse<string>("Review updated (pending approval)", HttpStatusCode.OK);
    }

    public async Task<BaseResponse<string>> DeleteByIdAsync(Guid reviewId)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
            return new BaseResponse<string>("Unauthorized", HttpStatusCode.Unauthorized);

        var review = await _context.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId && !r.IsDeleted);
        if (review is null) return new BaseResponse<string>("Review not found", HttpStatusCode.NotFound);

        // owner tələb et (başqasını silmək üçün controller-də ayrıca policy qoyacaqsan)
        if (review.UserId != userId)
            return new BaseResponse<string>("Forbidden", HttpStatusCode.Forbidden);

        review.IsDeleted = true;
        review.UpdatedAt = DateTime.UtcNow;
        _context.Reviews.Update(review);
        await _context.SaveChangesAsync();
        return new BaseResponse<string>("Review deleted", HttpStatusCode.OK);
    }

    public async Task<BaseResponse<string>> ApproveReviewAsync(Guid reviewId)
    {
        // Controller policy bunu artıq qoruyur
        var review = await _context.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId && !r.IsDeleted);
        if (review is null) return new BaseResponse<string>("Review not found", HttpStatusCode.NotFound);

        review.IsApproved = true;
        review.ApprovedAt = DateTime.UtcNow;
        review.UpdatedAt = DateTime.UtcNow;

        _context.Reviews.Update(review);
        await _context.SaveChangesAsync();
        return new BaseResponse<string>("Review approved", HttpStatusCode.OK);
    }

    public async Task<BaseResponse<List<ReviewGetDto>>> GetApprovedReviewsAsync(ReviewQueryDto query)
    {
        // sanitize pagination
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 20 : (query.PageSize > 100 ? 100 : query.PageSize);

        var q = _context.Reviews
            .Include(r => r.User)
            .Where(r => !r.IsDeleted && r.IsApproved);

        if (query.GymId.HasValue)
            q = q.Where(r => r.GymID == query.GymId);
        if (query.TrainerId.HasValue)
            q = q.Where(r => r.TrainerId == query.TrainerId);
        if (query.SupplementId.HasValue)
            q = q.Where(r => r.SupplementId == query.SupplementId);

        var list = await q.AsNoTracking()
            .OrderByDescending(r => r.ApprovedAt)
            .ThenByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ReviewGetDto
            {
                Id = r.Id,
                IsApproved = r.IsApproved,
                ApprovedAt = r.ApprovedAt,
                GymId = r.GymID,
                TrainerId = r.TrainerId,
                SupplementId = r.SupplementId,
                UserId = r.UserId,
                UserFullName = r.User != null ? (r.User.FullName ?? r.User.UserName) : null,
                Content = r.Content,
                Rating = r.Rating,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();

        if (list.Count == 0)
            return new BaseResponse<List<ReviewGetDto>>("No approved reviews found", null, HttpStatusCode.NotFound);

        return new BaseResponse<List<ReviewGetDto>>("Approved reviews retrieved", list, HttpStatusCode.OK);
    }

    public async Task<BaseResponse<decimal>> GetAverageRatingAsync(ReviewQueryDto query)
    {
        if (!SingleTarget(query.GymId, query.TrainerId, query.SupplementId))
            return new BaseResponse<decimal>("Exactly one target (Gym/Trainer/Supplement) is required",
                                             0m, HttpStatusCode.BadRequest);

        var q = _context.Reviews.Where(r => !r.IsDeleted && r.IsApproved);

        if (query.GymId.HasValue) q = q.Where(r => r.GymID == query.GymId);
        if (query.TrainerId.HasValue) q = q.Where(r => r.TrainerId == query.TrainerId);
        if (query.SupplementId.HasValue) q = q.Where(r => r.SupplementId == query.SupplementId);

        var any = await q.AnyAsync();
        if (!any)
            return new BaseResponse<decimal>("No approved reviews", 0m, HttpStatusCode.NotFound);

        var avg = await q.AverageAsync(r => (decimal)r.Rating);
        return new BaseResponse<decimal>("Average rating retrieved", decimal.Round(avg, 2), HttpStatusCode.OK);
    }

    public async Task<BaseResponse<bool>> HasUserReviewedAsync(ReviewCreateDto dto)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
            return new BaseResponse<bool>("Unauthorized", false, HttpStatusCode.Unauthorized);

        if (!SingleTarget(dto.GymId, dto.TrainerId, dto.SupplementId))
            return new BaseResponse<bool>("Exactly one target (Gym/Trainer/Supplement) is required",
                                          false, HttpStatusCode.BadRequest);

        var exists = await _context.Reviews.AnyAsync(r =>
            !r.IsDeleted &&
            r.UserId == userId &&
            r.GymID == dto.GymId &&
            r.TrainerId == dto.TrainerId &&
            r.SupplementId == dto.SupplementId);

        return new BaseResponse<bool>("Checked", exists, HttpStatusCode.OK);
    }

    public async Task<BaseResponse<List<ReviewGetDto>>> GetMyReviewsAsync()
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
            return new BaseResponse<List<ReviewGetDto>>("Unauthorized", null, HttpStatusCode.Unauthorized);

        var list = await _context.Reviews
            .Where(r => !r.IsDeleted && r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReviewGetDto
            {
                Id = r.Id,
                IsApproved = r.IsApproved,
                ApprovedAt = r.ApprovedAt,
                GymId = r.GymID,
                TrainerId = r.TrainerId,
                SupplementId = r.SupplementId,
                UserId = r.UserId,
                UserFullName = null, // optional
                Content = r.Content,
                Rating = r.Rating,
                CreatedAt = r.CreatedAt
            })
            .AsNoTracking()
            .ToListAsync();

        if (list.Count == 0)
            return new BaseResponse<List<ReviewGetDto>>("You have no reviews", null, HttpStatusCode.NotFound);

        return new BaseResponse<List<ReviewGetDto>>("My reviews retrieved", list, HttpStatusCode.OK);
    }

    public async Task<BaseResponse<string>> ClearAllMyReviewsAsync()
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
            return new BaseResponse<string>("Unauthorized", HttpStatusCode.Unauthorized);

        var myReviews = await _context.Reviews
            .Where(r => r.UserId == userId && !r.IsDeleted)
            .ToListAsync();

        if (myReviews.Count == 0)
            return new BaseResponse<string>("You have no reviews to clear", HttpStatusCode.NotFound);

        foreach (var r in myReviews)
        {
            r.IsDeleted = true;
            r.UpdatedAt = DateTime.UtcNow;
        }

        _context.Reviews.UpdateRange(myReviews);
        await _context.SaveChangesAsync();

        return new BaseResponse<string>("All your reviews have been deleted", HttpStatusCode.OK);
    }
}
