using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NFIT.Application.Abstracts.Services;
using NFIT.Application.DTOs.GymDtos;
using NFIT.Application.Shared;
using NFIT.Domain.Entities;
using NFIT.Persistence.Contexts;

namespace NFIT.Persistence.Services;

public class GymService:IGymService
{
    private readonly NFITDbContext _context;
    private readonly IFileService _fileService;

    public GymService(NFITDbContext context, IFileService fileService)
    {
        _context = context;
        _fileService = fileService;
    }

    // CREATE
    public async Task<BaseResponse<Guid>> CreateAsync(GymCreateDto dto)
    {
        // District check
        var districtExists = await _context.Districts.AnyAsync(d => d.Id == dto.DistrictId && !d.IsDeleted);
        if (!districtExists)
            return new BaseResponse<Guid>("District not found", Guid.Empty, HttpStatusCode.BadRequest);

        // Unique (Name + District)
        var nameExists = await _context.Gyms
            .AnyAsync(g => !g.IsDeleted &&
                           g.DistrictId == dto.DistrictId &&
                           g.Name != null &&
                           g.Name.ToLower() == dto.Name.ToLower());
        if (nameExists)
            return new BaseResponse<Guid>("Gym with same name already exists in this district", Guid.Empty, HttpStatusCode.Conflict);

        // Validate CategoryIds
        if (dto.CategoryIds.Count > 0)
        {
            var found = await _context.Categories
                .Where(c => !c.IsDeleted && dto.CategoryIds.Contains(c.Id))
                .Select(c => c.Id)
                .ToListAsync();

            if (dto.CategoryIds.Except(found).Any())
                return new BaseResponse<Guid>("Some CategoryIds were not found", Guid.Empty, HttpStatusCode.BadRequest);
        }

        // Validate SubscriptionPlanIds
        if (dto.SubscriptionPlanIds.Count > 0)
        {
            var found = await _context.SubscriptionPlans
                .Where(s => !s.IsDeleted && dto.SubscriptionPlanIds.Contains(s.Id))
                .Select(s => s.Id)
                .ToListAsync();

            if (dto.SubscriptionPlanIds.Except(found).Any())
                return new BaseResponse<Guid>("Some SubscriptionPlanIds were not found", Guid.Empty, HttpStatusCode.BadRequest);
        }

        var id = Guid.NewGuid();
        var gym = new Gym
        {
            Id = id,
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
            Address = dto.Address.Trim(),
            DistrictId = dto.DistrictId,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            Phone = dto.Phone?.Trim(),
            Email = dto.Email?.Trim(),
            Website = dto.Website?.Trim(),
            InstagramLink = dto.InstagramLink?.Trim(),
            IsPremium = dto.IsPremium,
            IsActive = dto.IsActive,
            Rating = 0m,
            GymCategories = new List<GymCategory>(),
            AvailableSubscriptions = new List<SubscriptionPlan>(),
            Reviews = new List<Review>(),
            Images = new List<Image>(),
            CheckIns = new List<GymCheckIn>()
        };

        // join: GymCategories
        foreach (var catId in dto.CategoryIds.Distinct())
            gym.GymCategories.Add(new GymCategory { GymId = id, CategoryId = catId });

        // collection: AvailableSubscriptions (attach existing)
        if (dto.SubscriptionPlanIds.Count > 0)
        {
            var subs = await _context.SubscriptionPlans
                .Where(s => dto.SubscriptionPlanIds.Contains(s.Id) && !s.IsDeleted)
                .ToListAsync();

            foreach (var sp in subs)
                gym.AvailableSubscriptions.Add(sp);
        }

        await _context.Gyms.AddAsync(gym);
        await _context.SaveChangesAsync();

        return new BaseResponse<Guid>("Gym created", id, HttpStatusCode.Created);
    }

    // UPDATE
    public async Task<BaseResponse<string>> UpdateAsync(GymUpdateDto dto)
    {
        var gym = await _context.Gyms
            .Include(g => g.GymCategories)
            .Include(g => g.AvailableSubscriptions)
            .FirstOrDefaultAsync(g => g.Id == dto.Id && !g.IsDeleted);

        if (gym == null)
            return new BaseResponse<string>("Gym not found", HttpStatusCode.NotFound);

        var districtExists = await _context.Districts.AnyAsync(d => d.Id == dto.DistrictId && !d.IsDeleted);
        if (!districtExists)
            return new BaseResponse<string>("District not found", HttpStatusCode.BadRequest);

        // unique name per district (ignore self)
        var nameExists = await _context.Gyms.AnyAsync(g =>
            !g.IsDeleted &&
            g.Id != dto.Id &&
            g.DistrictId == dto.DistrictId &&
            g.Name != null &&
            g.Name.ToLower() == dto.Name.ToLower());
        if (nameExists)
            return new BaseResponse<string>("Gym with same name already exists in this district", HttpStatusCode.Conflict);

        // validate references
        if (dto.CategoryIds.Count > 0)
        {
            var found = await _context.Categories
                .Where(c => !c.IsDeleted && dto.CategoryIds.Contains(c.Id))
                .Select(c => c.Id)
                .ToListAsync();
            if (dto.CategoryIds.Except(found).Any())
                return new BaseResponse<string>("Some CategoryIds were not found", HttpStatusCode.BadRequest);
        }

        if (dto.SubscriptionPlanIds.Count > 0)
        {
            var found = await _context.SubscriptionPlans
                .Where(s => !s.IsDeleted && dto.SubscriptionPlanIds.Contains(s.Id))
                .Select(s => s.Id)
                .ToListAsync();
            if (dto.SubscriptionPlanIds.Except(found).Any())
                return new BaseResponse<string>("Some SubscriptionPlanIds were not found", HttpStatusCode.BadRequest);
        }

        // map scalar
        gym.Name = dto.Name.Trim();
        gym.Description = dto.Description?.Trim();
        gym.Address = dto.Address.Trim();
        gym.DistrictId = dto.DistrictId;
        gym.Latitude = dto.Latitude;
        gym.Longitude = dto.Longitude;
        gym.Phone = dto.Phone?.Trim();
        gym.Email = dto.Email?.Trim();
        gym.Website = dto.Website?.Trim();
        gym.InstagramLink = dto.InstagramLink?.Trim();
        gym.IsPremium = dto.IsPremium;
        gym.IsActive = dto.IsActive;
        gym.UpdatedAt = DateTime.UtcNow;

        // ------- sync categories (SOFT DELETE ilə) -------
        var newCatIds = dto.CategoryIds.Distinct().ToHashSet();
        gym.GymCategories ??= new List<GymCategory>();

        // soft-remove köhnələr
        var toRemove = gym.GymCategories
            .Where(gc => !gc.IsDeleted && !newCatIds.Contains(gc.CategoryId))
            .ToList();

        foreach (var rc in toRemove)
        {
            rc.IsDeleted = true;
            rc.UpdatedAt = DateTime.UtcNow;
        }
        if (toRemove.Count > 0) _context.GymCategories.UpdateRange(toRemove);

        // add missing (yalnız living categories)
        var existingCatIds = gym.GymCategories
            .Where(gc => !gc.IsDeleted)
            .Select(gc => gc.CategoryId)
            .ToHashSet();

        var idsToAdd = newCatIds.Except(existingCatIds).ToList();
        if (idsToAdd.Count > 0)
        {
            var validCatIds = await _context.Categories
                .Where(c => idsToAdd.Contains(c.Id) && !c.IsDeleted)
                .Select(c => c.Id)
                .ToListAsync();

            foreach (var catId in validCatIds)
                gym.GymCategories.Add(new GymCategory { GymId = gym.Id, CategoryId = catId });
        }

        // ------- sync subscriptions (collection) -------
        var newSubIds = dto.SubscriptionPlanIds.Distinct().ToHashSet();

        // remove old (filter living only)
        gym.AvailableSubscriptions = gym.AvailableSubscriptions
            .Where(s => newSubIds.Contains(s.Id))
            .ToList();

        // add missing
        var existingSubIds = gym.AvailableSubscriptions.Select(s => s.Id).ToHashSet();
        var subsToAdd = await _context.SubscriptionPlans
            .Where(s => newSubIds.Except(existingSubIds).Contains(s.Id) && !s.IsDeleted)
            .ToListAsync();
        foreach (var sp in subsToAdd)
            gym.AvailableSubscriptions.Add(sp);

        _context.Gyms.Update(gym);
        await _context.SaveChangesAsync();

        return new BaseResponse<string>("Gym updated", HttpStatusCode.OK);
    }
    // SOFT DELETE
    public async Task<BaseResponse<string>> DeleteAsync(Guid id)
    {
        var gym = await _context.Gyms.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (gym == null)
            return new BaseResponse<string>("Gym not found", HttpStatusCode.NotFound);

        gym.IsDeleted = true;
        gym.IsActive = false;
        gym.UpdatedAt = DateTime.UtcNow;

        _context.Gyms.Update(gym);
        await _context.SaveChangesAsync();

        return new BaseResponse<string>("Gym deleted (soft)", HttpStatusCode.OK);
    }

    // GET BY ID (details)
    public async Task<BaseResponse<GymDetailsDto>> GetByIdAsync(Guid id)
    {
        var gym = await _context.Gyms
            .Include(g => g.District)
            .Include(g => g.GymCategories)
            .Include(g => g.AvailableSubscriptions)
            .FirstOrDefaultAsync(g => g.Id == id && !g.IsDeleted);

        if (gym == null)
            return new BaseResponse<GymDetailsDto>("Gym not found", null, HttpStatusCode.NotFound);

        // category names
        var categoryIds = gym.GymCategories?.Select(gc => gc.CategoryId).ToList() ?? new();
        var categoryNames = await _context.GymCategories
     .Where(gc => gc.GymId == gym.Id && !gc.IsDeleted && !gc.Category.IsDeleted)
     .Select(gc => gc.Category.Name)
     .ToListAsync();

        var dto = new GymDetailsDto
        {
            Id = gym.Id,
            Name = gym.Name!,
            Description = gym.Description,
            Address = gym.Address!,
            DistrictId = gym.DistrictId,
            DistrictName = gym.District?.Name ?? "",
            Latitude = gym.Latitude,
            Longitude = gym.Longitude,
            Phone = gym.Phone,
            Email = gym.Email,
            Website = gym.Website,
            InstagramLink = gym.InstagramLink,
            IsPremium = gym.IsPremium,
            IsActive = gym.IsActive,
            Rating = gym.Rating,
            Categories = categoryNames,
            Subscriptions = gym.AvailableSubscriptions?
                 .Select(s => s.Name)
                 .ToList() ?? new List<string>()
        };

        return new BaseResponse<GymDetailsDto>("Gym retrieved", dto, HttpStatusCode.OK);
    }

    // GET ALL (pagination + lightweight projection)
    public async Task<BaseResponse<List<GymListItemDto>>> GetAllAsync(int page = 1, int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var query = _context.Gyms
            .Include(g => g.District)
            .Include(g => g.GymCategories)
            .Include(g => g.AvailableSubscriptions)
            .Where(g => !g.IsDeleted)
            .OrderByDescending(g => g.IsPremium)
            .ThenBy(g => g.Name);

        var list = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(g => new GymListItemDto
            {
                Id = g.Id,
                Name = g.Name!,
                Address = g.Address!,
                DistrictName = g.District!.Name,
                IsPremium = g.IsPremium,
                IsActive = g.IsActive,
                Rating = g.Rating,
                CategoryCount = g.GymCategories.Count(gc => !gc.IsDeleted && !gc.Category.IsDeleted),
                SubscriptionCount = g.AvailableSubscriptions.Count
            })
            .ToListAsync();

        if (list.Count == 0)
            return new BaseResponse<List<GymListItemDto>>("No gyms found", null, HttpStatusCode.NotFound);

        return new BaseResponse<List<GymListItemDto>>("Gyms retrieved", list, HttpStatusCode.OK);
    }
    public async Task<BaseResponse<string>> AddImageAsync(Guid gymId, IFormFile image)
    {
        if (image == null || image.Length == 0)
            return new BaseResponse<string>("Image is required", HttpStatusCode.BadRequest);

        var gym = await _context.Gyms
            .Include(g => g.Images)
            .FirstOrDefaultAsync(g => g.Id == gymId && !g.IsDeleted);

        if (gym == null)
            return new BaseResponse<string>("Gym not found", HttpStatusCode.NotFound);

        // Faylı upload et
        var url = await _fileService.UploadAsync(image);

        var img = new Image
        {
            Id = Guid.NewGuid(),
            ImageUrl = url,       // səndə "Url" deyədirsə ona uyğunlaşdır
            GymId = gymId,        // ⚠️ Image entity-də GymId sahəsi olmalıdır
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        gym.Images ??= new List<Image>();
        gym.Images.Add(img);

        _context.Gyms.Update(gym);
        await _context.SaveChangesAsync();

        return new BaseResponse<string>("Image added to gym", HttpStatusCode.Created);
    }

    public async Task<BaseResponse<string>> DeleteImageAsync(Guid gymId, Guid imageId)
    {
        var gym = await _context.Gyms
            .Include(g => g.Images)
            .FirstOrDefaultAsync(g => g.Id == gymId && !g.IsDeleted);

        if (gym == null)
            return new BaseResponse<string>("Gym not found", HttpStatusCode.NotFound);

        var image = gym.Images?.FirstOrDefault(i => i.Id == imageId && !i.IsDeleted);
        if (image == null)
            return new BaseResponse<string>("Image not found", HttpStatusCode.NotFound);

        image.IsDeleted = true;
        image.UpdatedAt = DateTime.UtcNow;

        _context.Images.Update(image); // varsa DbSet<Image> _context.Images
        await _context.SaveChangesAsync();

        return new BaseResponse<string>("Image deleted from gym", HttpStatusCode.OK);
    }
}
