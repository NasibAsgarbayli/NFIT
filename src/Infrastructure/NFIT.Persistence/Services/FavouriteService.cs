using System.Linq.Expressions;
using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NFIT.Application.Abstracts.Repositories;
using NFIT.Application.Abstracts.Services;
using NFIT.Application.DTOs.FavouriteDtos;
using NFIT.Application.Shared;
using NFIT.Domain.Entities;
using NFIT.Persistence.Contexts;

namespace NFIT.Persistence.Services;

public class FavouriteService:IFavouriteService
{
    private readonly IFavouriteRepository _favRepo;
    private readonly IHttpContextAccessor _http;

    public FavouriteService(IFavouriteRepository favRepo, IHttpContextAccessor http)
    {
        _favRepo = favRepo;
        _http = http;
    }

    // ===== Helpers =====
    private string? GetCurrentUserId()
    {
        var user = _http.HttpContext?.User;
        if (user is null || user.Identity?.IsAuthenticated != true) return null;

        return user.FindFirstValue(ClaimTypes.NameIdentifier)   // Identity
            ?? user.FindFirst("sub")?.Value;                    // JWT (sub)
    }

    private static bool HasSingleTarget(FavouriteAddDto dto)
    {
        var count = 0;
        if (dto.GymId.HasValue) count++;
        if (dto.TrainerId.HasValue) count++;
        if (dto.SupplementId.HasValue) count++;
        return count == 1;
    }

    // ===== CREATE =====
    public async Task<BaseResponse<string>> CreateAsync(FavouriteAddDto dto)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return new BaseResponse<string>("Unauthorized", HttpStatusCode.Unauthorized);

        if (!HasSingleTarget(dto))
            return new BaseResponse<string>("Exactly one of GymId / TrainerId / SupplementId must be provided", HttpStatusCode.BadRequest);

        var existing = await _favRepo
            .GetAllFiltered(f =>
                f.UserId == userId &&
                f.GymId == dto.GymId &&
                f.TrainerId == dto.TrainerId &&
                f.SupplementId == dto.SupplementId,
                IsTracking: true)
            .FirstOrDefaultAsync();

        if (existing != null)
        {
            if (!existing.IsDeleted)
                return new BaseResponse<string>("Already in favourites", HttpStatusCode.Conflict);

            existing.IsDeleted = false;
            existing.AddedAt = DateTime.UtcNow;

            _favRepo.Update(existing);
            await _favRepo.SaveChangeAsync();
            return new BaseResponse<string>("Re-added to favourites", HttpStatusCode.Created);
        }

        var fav = new Favourite
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            GymId = dto.GymId,
            TrainerId = dto.TrainerId,
            SupplementId = dto.SupplementId,
            AddedAt = DateTime.UtcNow
        };

        await _favRepo.AddAsync(fav);
        await _favRepo.SaveChangeAsync();

        return new BaseResponse<string>("Added to favourites", HttpStatusCode.Created);
    }

    // ===== DELETE BY ID (soft) =====
    public async Task<BaseResponse<string>> DeleteByIdAsync(Guid favouriteId)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return new BaseResponse<string>("Unauthorized", HttpStatusCode.Unauthorized);

        var favourite = await _favRepo
            .GetAllFiltered(f => f.Id == favouriteId && f.UserId == userId && !f.IsDeleted, IsTracking: true)
            .FirstOrDefaultAsync();

        if (favourite == null)
            return new BaseResponse<string>("Favourite not found", HttpStatusCode.NotFound);

        favourite.IsDeleted = true;
        favourite.UpdatedAt = DateTime.UtcNow;

        _favRepo.Update(favourite);
        await _favRepo.SaveChangeAsync();

        return new BaseResponse<string>("Favourite deleted", HttpStatusCode.OK);
    }

    // ===== GET ALL (current user) =====
    public async Task<BaseResponse<FavouriteListResponseDto>> GetAllMyFavouriteAsync()
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return new BaseResponse<FavouriteListResponseDto>("Unauthorized", null, HttpStatusCode.Unauthorized);

        // ilkin include-lar
        var query = _favRepo.GetAllFiltered(
            predicate: f => f.UserId == userId && !f.IsDeleted,
            include: new Expression<Func<Favourite, object>>[] { f => f.Gym, f => f.Trainer, f => f.Supplement },
            IsTracking: false
        )
        // daha dərin include-lar (District)
        .Include(f => f.Gym).ThenInclude(g => g.District);

        var totalCount = await query.CountAsync();

        var list = await query
            .OrderByDescending(f => f.AddedAt)
            .ToListAsync();

        var items = list.Select(f => new FavouriteListItemDto
        {
            Id = f.Id,
            Type = f.GymId.HasValue ? "Gym" :
                   f.TrainerId.HasValue ? "Trainer" : "Supplement",
            EntityId = f.GymId ?? f.TrainerId ?? f.SupplementId ?? Guid.Empty,
            Name = f.Gym != null
                     ? (f.Gym.Name ?? "")
                     : f.Trainer != null
                         ? ((f.Trainer.FirstName + " " + f.Trainer.LastName).Trim())
                         : f.Supplement != null
                             ? (f.Supplement.Name ?? "")
                             : "",
            AddedAt = f.AddedAt
        }).ToList();

        if (items.Count == 0)
            return new BaseResponse<FavouriteListResponseDto>("No favourites found", null, HttpStatusCode.NotFound);

        var payload = new FavouriteListResponseDto { TotalCount = totalCount, Items = items };
        return new BaseResponse<FavouriteListResponseDto>("Favourites retrieved", payload, HttpStatusCode.OK);
    }

    // ===== Only Gyms =====
    public async Task<BaseResponse<List<FavouriteListItemDto>>> GetUserFavouriteGymsAsync()
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return new BaseResponse<List<FavouriteListItemDto>>("Unauthorized", null, HttpStatusCode.Unauthorized);

        var list = await _favRepo
            .GetAllFiltered(f =>
                f.UserId == userId &&
                !f.IsDeleted &&
                f.GymId != null &&
                f.Gym != null &&
                !f.Gym.IsDeleted,
                include: new Expression<Func<Favourite, object>>[] { f => f.Gym },
                IsTracking: false)
            .OrderByDescending(f => f.AddedAt)
            .Select(f => new FavouriteListItemDto
            {
                Id = f.Id,
                Type = "Gym",
                EntityId = f.GymId!.Value,
                Name = f.Gym!.Name ?? "",
                AddedAt = f.AddedAt
            })
            .ToListAsync();

        if (list.Count == 0)
            return new BaseResponse<List<FavouriteListItemDto>>("No favourite gyms", null, HttpStatusCode.NotFound);

        return new BaseResponse<List<FavouriteListItemDto>>("Favourite gyms retrieved", list, HttpStatusCode.OK);
    }

    // ===== Only Trainers =====
    public async Task<BaseResponse<List<FavouriteListItemDto>>> GetUserFavouriteTrainersAsync()
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return new BaseResponse<List<FavouriteListItemDto>>("Unauthorized", null, HttpStatusCode.Unauthorized);

        var list = await _favRepo
            .GetAllFiltered(f =>
                f.UserId == userId &&
                !f.IsDeleted &&
                f.TrainerId != null &&
                f.Trainer != null &&
                !f.Trainer.IsDeleted,
                include: new Expression<Func<Favourite, object>>[] { f => f.Trainer },
                IsTracking: false)
            .OrderByDescending(f => f.AddedAt)
            .Select(f => new FavouriteListItemDto
            {
                Id = f.Id,
                Type = "Trainer",
                EntityId = f.TrainerId!.Value,
                Name = (f.Trainer!.FirstName + " " + f.Trainer!.LastName).Trim(),
                AddedAt = f.AddedAt
            })
            .ToListAsync();

        if (list.Count == 0)
            return new BaseResponse<List<FavouriteListItemDto>>("No favourite trainers", null, HttpStatusCode.NotFound);

        return new BaseResponse<List<FavouriteListItemDto>>("Favourite trainers retrieved", list, HttpStatusCode.OK);
    }

    // ===== Only Supplements =====
    public async Task<BaseResponse<List<FavouriteListItemDto>>> GetUserFavouriteSupplementsAsync()
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return new BaseResponse<List<FavouriteListItemDto>>("Unauthorized", null, HttpStatusCode.Unauthorized);

        var list = await _favRepo
            .GetAllFiltered(f =>
                f.UserId == userId &&
                !f.IsDeleted &&
                f.SupplementId != null &&
                f.Supplement != null &&
                !f.Supplement.IsDeleted,
                include: new Expression<Func<Favourite, object>>[] { f => f.Supplement },
                IsTracking: false)
            .OrderByDescending(f => f.AddedAt)
            .Select(f => new FavouriteListItemDto
            {
                Id = f.Id,
                Type = "Supplement",
                EntityId = f.SupplementId!.Value,
                Name = f.Supplement!.Name ?? "",
                AddedAt = f.AddedAt
            })
            .ToListAsync();

        if (list.Count == 0)
            return new BaseResponse<List<FavouriteListItemDto>>("No favourite supplements", null, HttpStatusCode.NotFound);

        return new BaseResponse<List<FavouriteListItemDto>>("Favourite supplements retrieved", list, HttpStatusCode.OK);
    }

    // ===== Clear All (current user) =====
    public async Task<BaseResponse<string>> ClearUserFavouritesAsync()
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return new BaseResponse<string>("Unauthorized", HttpStatusCode.Unauthorized);

        var items = await _favRepo
            .GetAllFiltered(f => f.UserId == userId && !f.IsDeleted, IsTracking: true)
            .ToListAsync();

        if (items.Count == 0)
            return new BaseResponse<string>("No favourites to clear", HttpStatusCode.NotFound);

        foreach (var f in items)
        {
            f.IsDeleted = true;
            f.UpdatedAt = DateTime.UtcNow;
            _favRepo.Update(f); // UpdateRange yoxdursa, tək-tək update
        }

        await _favRepo.SaveChangeAsync();
        return new BaseResponse<string>("All favourites cleared", HttpStatusCode.OK);
    }
}
