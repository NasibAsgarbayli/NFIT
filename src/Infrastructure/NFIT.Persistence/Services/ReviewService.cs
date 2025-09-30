using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NFIT.Application.Abstracts.Repositories;
using NFIT.Application.Abstracts.Services;
using NFIT.Application.DTOs.ReviewDtos;
using NFIT.Application.Shared;
using NFIT.Domain.Entities;
using NFIT.Persistence.Contexts;

namespace NFIT.Persistence.Services;

public class ReviewService:IReviewService
{
    private readonly IReviewRepository _reviews;
    private readonly IGymRepository _gyms;
    private readonly ITrainerRepository _trainers;
    private readonly ISupplementRepository _supplements;
    private readonly IHttpContextAccessor _http;

    public ReviewService(
        IReviewRepository reviews,
        IGymRepository gyms,
        ITrainerRepository trainers,
        ISupplementRepository supplements,
        IHttpContextAccessor http)
    {
        _reviews = reviews;
        _gyms = gyms;
        _trainers = trainers;
        _supplements = supplements;
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

    // ================= CREATE =================
    public async Task<BaseResponse<Guid>> CreateAsync(ReviewCreateDto dto)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
            return new("Unauthorized", Guid.Empty, HttpStatusCode.Unauthorized);

        if (!SingleTarget(dto.GymId, dto.TrainerId, dto.SupplementId))
            return new("Exactly one of GymId/TrainerId/SupplementId must be provided", Guid.Empty, HttpStatusCode.BadRequest);

        // duplicate
        var exists = await _reviews.GetByFiltered(r =>
            !r.IsDeleted &&
            r.UserId == userId &&
            r.GymID == dto.GymId &&
            r.TrainerId == dto.TrainerId &&
            r.SupplementId == dto.SupplementId, IsTracking: false).AnyAsync();
        if (exists)
            return new("You have already reviewed this target", Guid.Empty, HttpStatusCode.Conflict);

        // (istəsən: target-in həqiqətən mövcudluğunu da yoxlaya bilərsən)
        var entity = new Review
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            GymID = dto.GymId,
            TrainerId = dto.TrainerId,
            SupplementId = dto.SupplementId,
            Content = dto.Content?.Trim(),
            Rating = dto.Rating,
            IsApproved = false,
            ApprovedAt = null,
            CreatedAt = DateTime.UtcNow
        };

        await _reviews.AddAsync(entity);
        await _reviews.SaveChangeAsync();

        return new("Review created (pending approval)", entity.Id, HttpStatusCode.Created);
    }

    // ================= UPDATE =================
    public async Task<BaseResponse<string>> UpdateAsync(ReviewUpdateDto dto)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
            return new("Unauthorized", HttpStatusCode.Unauthorized);

        var review = await _reviews.GetByFiltered(r => r.Id == dto.Id && !r.IsDeleted).FirstOrDefaultAsync();
        if (review is null) return new("Review not found", HttpStatusCode.NotFound);
        if (review.UserId != userId) return new("Forbidden", HttpStatusCode.Forbidden);

        review.Content = dto.Content?.Trim();
        review.Rating = dto.Rating;
        review.IsApproved = false;
        review.ApprovedAt = null;
        review.UpdatedAt = DateTime.UtcNow;

        _reviews.Update(review);
        await _reviews.SaveChangeAsync();

        return new("Review updated (pending approval)", HttpStatusCode.OK);
    }

    // ================= DELETE (soft by owner) =================
    public async Task<BaseResponse<string>> DeleteByIdAsync(Guid reviewId)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
            return new("Unauthorized", HttpStatusCode.Unauthorized);

        var review = await _reviews.GetByFiltered(r => r.Id == reviewId && !r.IsDeleted).FirstOrDefaultAsync();
        if (review is null) return new("Review not found", HttpStatusCode.NotFound);
        if (review.UserId != userId) return new("Forbidden", HttpStatusCode.Forbidden);

        review.IsDeleted = true;
        review.UpdatedAt = DateTime.UtcNow;

        _reviews.Update(review);
        await _reviews.SaveChangeAsync();

        return new("Review deleted", HttpStatusCode.OK);
    }

    // ================= APPROVE (admin policy ilə qorunur) =================
    public async Task<BaseResponse<string>> ApproveReviewAsync(Guid reviewId)
    {
        var review = await _reviews.GetByFiltered(r => r.Id == reviewId && !r.IsDeleted).FirstOrDefaultAsync();
        if (review is null) return new("Review not found", HttpStatusCode.NotFound);

        review.IsApproved = true;
        review.ApprovedAt = DateTime.UtcNow;
        review.UpdatedAt = DateTime.UtcNow;

        _reviews.Update(review);
        await _reviews.SaveChangeAsync();

        return new("Review approved", HttpStatusCode.OK);
    }

    // ================= LIST: APPROVED (paged) =================
    public async Task<BaseResponse<List<ReviewGetDto>>> GetApprovedReviewsAsync(ReviewQueryDto query)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 20 : (query.PageSize > 100 ? 100 : query.PageSize);

        var q = _reviews
            .GetByFiltered(r => !r.IsDeleted && r.IsApproved,
                include: new[] { (System.Linq.Expressions.Expression<Func<Review, object>>)(r => r.User) },
                IsTracking: false);

        if (query.GymId.HasValue) q = q.Where(r => r.GymID == query.GymId);
        if (query.TrainerId.HasValue) q = q.Where(r => r.TrainerId == query.TrainerId);
        if (query.SupplementId.HasValue) q = q.Where(r => r.SupplementId == query.SupplementId);

        var list = await q
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
            return new("No approved reviews found", null, HttpStatusCode.NotFound);

        return new("Approved reviews retrieved", list, HttpStatusCode.OK);
    }

    // ================= AVG RATING =================
    public async Task<BaseResponse<decimal>> GetAverageRatingAsync(ReviewQueryDto query)
    {
        if (!SingleTarget(query.GymId, query.TrainerId, query.SupplementId))
            return new("Exactly one target (Gym/Trainer/Supplement) is required", 0m, HttpStatusCode.BadRequest);

        var q = _reviews.GetByFiltered(r => !r.IsDeleted && r.IsApproved, IsTracking: false);

        if (query.GymId.HasValue) q = q.Where(r => r.GymID == query.GymId);
        if (query.TrainerId.HasValue) q = q.Where(r => r.TrainerId == query.TrainerId);
        if (query.SupplementId.HasValue) q = q.Where(r => r.SupplementId == query.SupplementId);

        var any = await q.AnyAsync();
        if (!any) return new("No approved reviews", 0m, HttpStatusCode.NotFound);

        var avg = await q.AverageAsync(r => (decimal)r.Rating);
        return new("Average rating retrieved", decimal.Round(avg, 2), HttpStatusCode.OK);
    }

    // ================= CHECK DUPLICATE (me) =================
    public async Task<BaseResponse<bool>> HasUserReviewedAsync(ReviewCreateDto dto)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
            return new("Unauthorized", false, HttpStatusCode.Unauthorized);

        if (!SingleTarget(dto.GymId, dto.TrainerId, dto.SupplementId))
            return new("Exactly one target (Gym/Trainer/Supplement) is required", false, HttpStatusCode.BadRequest);

        var exists = await _reviews.GetByFiltered(r =>
            !r.IsDeleted &&
            r.UserId == userId &&
            r.GymID == dto.GymId &&
            r.TrainerId == dto.TrainerId &&
            r.SupplementId == dto.SupplementId, IsTracking: false).AnyAsync();

        return new("Checked", exists, HttpStatusCode.OK);
    }

    // ================= LIST: MY REVIEWS =================
    public async Task<BaseResponse<List<ReviewGetDto>>> GetMyReviewsAsync()
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
            return new("Unauthorized", null, HttpStatusCode.Unauthorized);

        var list = await _reviews
            .GetByFiltered(r => !r.IsDeleted && r.UserId == userId, IsTracking: false)
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
                UserFullName = null,
                Content = r.Content,
                Rating = r.Rating,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();

        if (list.Count == 0)
            return new("You have no reviews", null, HttpStatusCode.NotFound);

        return new("My reviews retrieved", list, HttpStatusCode.OK);
    }

    // ================= CLEAR ALL (MY) =================
    public async Task<BaseResponse<string>> ClearAllMyReviewsAsync()
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
            return new("Unauthorized", HttpStatusCode.Unauthorized);

        var myReviews = await _reviews
            .GetByFiltered(r => r.UserId == userId && !r.IsDeleted)
            .ToListAsync();

        if (myReviews.Count == 0)
            return new("You have no reviews to clear", HttpStatusCode.NotFound);

        foreach (var r in myReviews)
        {
            r.IsDeleted = true;
            r.UpdatedAt = DateTime.UtcNow;
        }

        foreach (var r in myReviews)
            _reviews.Update(r);

        await _reviews.SaveChangeAsync();

        return new("All your reviews have been deleted", HttpStatusCode.OK);
    }
}
