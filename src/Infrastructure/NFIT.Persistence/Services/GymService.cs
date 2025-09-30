using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NFIT.Application.Abstracts.Repositories;
using NFIT.Application.Abstracts.Services;
using NFIT.Application.DTOs.GymDtos;
using NFIT.Application.Shared;
using NFIT.Domain.Entities;
using NFIT.Persistence.Contexts;

namespace NFIT.Persistence.Services;

public class GymService:IGymService
{
    private readonly IGymRepository _gyms;
    private readonly IDistrictRepository _districts;
    private readonly ICategoryRepository _categories;
    private readonly ISubscriptionPlanRepository _subs;
    private readonly IImageRepository _images;
    // email göndərişi üçün
    private readonly IEmailService _emailService;
    private readonly ICloudinaryService _cloud;
    // İstifadəçilərin e-poçtlarını toplamaq üçün (DbContext əvəzi)
    private readonly UserManager<AppUser> _userManager;

    public GymService(
        IGymRepository gyms,
        IDistrictRepository districts,
        ICategoryRepository categories,
        ISubscriptionPlanRepository subs,
        IImageRepository images,
        IEmailService emailService,
        ICloudinaryService cloud,
        UserManager<AppUser> userManager)
    {
        _gyms = gyms;
        _districts = districts;
        _categories = categories;
        _subs = subs;
        _images = images;
        _emailService = emailService;
        _cloud = cloud;
        _userManager = userManager;
    }

    // ================= CREATE =================
    public async Task<BaseResponse<Guid>> CreateAsync(GymCreateDto dto)
    {
        if (dto == null)
            return new("Body is required", Guid.Empty, HttpStatusCode.BadRequest);

        if (string.IsNullOrWhiteSpace(dto.Name))
            return new("Name is required", Guid.Empty, HttpStatusCode.BadRequest);

        if (string.IsNullOrWhiteSpace(dto.Address))
            return new("Address is required", Guid.Empty, HttpStatusCode.BadRequest);

        var name = dto.Name.Trim();
        var address = dto.Address.Trim();
        if (name.Length is < 2 or > 120) return new("Name length must be between 2 and 120", Guid.Empty, HttpStatusCode.BadRequest);
        if (address.Length is < 5 or > 200) return new("Address length must be between 5 and 200", Guid.Empty, HttpStatusCode.BadRequest);

        if (dto.Latitude is < -90 or > 90) return new("Latitude must be between -90 and 90", Guid.Empty, HttpStatusCode.BadRequest);
        if (dto.Longitude is < -180 or > 180) return new("Longitude must be between -180 and 180", Guid.Empty, HttpStatusCode.BadRequest);
        if (!string.IsNullOrWhiteSpace(dto.Email) && !dto.Email.Contains('@')) return new("Invalid email", Guid.Empty, HttpStatusCode.BadRequest);

        // District check
        var districtExists = await _districts.GetByFiltered(d => d.Id == dto.DistrictId && !d.IsDeleted, IsTracking: false).AnyAsync();
        if (!districtExists) return new("District not found", Guid.Empty, HttpStatusCode.BadRequest);

        dto.CategoryIds ??= new();
        dto.SubscriptionPlanIds ??= new();

        // Unique (Name + District)
        var nameExists = await _gyms
            .GetByFiltered(g => !g.IsDeleted && g.DistrictId == dto.DistrictId && g.Name != null && g.Name.Trim().ToUpper() == name.ToUpper(), IsTracking: false)
            .AnyAsync();
        if (nameExists) return new("Gym with same name already exists in this district", Guid.Empty, HttpStatusCode.Conflict);

        // Validate CategoryIds
        var catIds = dto.CategoryIds.Distinct().ToList();
        if (catIds.Count > 0)
        {
            var found = await _categories.GetByFiltered(c => !c.IsDeleted && catIds.Contains(c.Id), IsTracking: false)
                                         .Select(c => c.Id).ToListAsync();
            if (catIds.Except(found).Any())
                return new("Some CategoryIds were not found", Guid.Empty, HttpStatusCode.BadRequest);
        }

        // Validate SubscriptionPlanIds
        var subIds = dto.SubscriptionPlanIds.Distinct().ToList();
        if (subIds.Count > 0)
        {
            var found = await _subs.GetByFiltered(s => !s.IsDeleted && subIds.Contains(s.Id), IsTracking: false)
                                   .Select(s => s.Id).ToListAsync();
            if (subIds.Except(found).Any())
                return new("Some SubscriptionPlanIds were not found", Guid.Empty, HttpStatusCode.BadRequest);
        }

        var id = Guid.NewGuid();
        var gym = new Gym
        {
            Id = id,
            Name = name,
            Description = dto.Description?.Trim(),
            Address = address,
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

        foreach (var catId in catIds)
            gym.GymCategories.Add(new GymCategory { GymId = id, CategoryId = catId });

        if (subIds.Count > 0)
        {
            var subs = await _subs.GetByFiltered(s => subIds.Contains(s.Id) && !s.IsDeleted).ToListAsync();
            foreach (var sp in subs)
                gym.AvailableSubscriptions.Add(sp);
        }

        await _gyms.AddAsync(gym);
        await _gyms.SaveChangeAsync();

        // Notification (best-effort)
        try
        {
            var allEmails = _userManager.Users
                .Where(u => u.IsActive && u.EmailConfirmed && u.Email != null)
                .Select(u => u.Email!)
                .ToList();

            if (allEmails.Count > 0)
            {
                var subject = $"Yeni Gym əlavə olundu: {gym.Name}";
                var body = $@"<p><strong>{gym.Name}</strong> artıq NFIT platformasında!</p>
                              <p>Daha ətraflı məlumat üçün tətbiqimizə daxil olun.</p>";
                await _emailService.SendEmailAsync(allEmails, subject, body);
            }
        }
        catch { /* loglama varsa et */ }

        return new("Gym created", id, HttpStatusCode.Created);
    }

    // ================= UPDATE =================
    public async Task<BaseResponse<string>> UpdateAsync(GymUpdateDto dto)
    {
        var gym = await _gyms
            .GetByFiltered(g => g.Id == dto.Id && !g.IsDeleted,
                           include: new[] { (System.Linq.Expressions.Expression<Func<Gym, object>>)(g => g.GymCategories), g => g.AvailableSubscriptions })
            .FirstOrDefaultAsync();

        if (gym == null) return new("Gym not found", HttpStatusCode.NotFound);

        var districtExists = await _districts.GetByFiltered(d => d.Id == dto.DistrictId && !d.IsDeleted, IsTracking: false).AnyAsync();
        if (!districtExists) return new("District not found", HttpStatusCode.BadRequest);

        var nameExists = await _gyms
            .GetByFiltered(g => !g.IsDeleted && g.Id != dto.Id && g.DistrictId == dto.DistrictId && g.Name != null && g.Name.Trim() == dto.Name.Trim(), IsTracking: false)
            .AnyAsync();
        if (nameExists) return new("Gym with same name already exists in this district", HttpStatusCode.Conflict);

        if (dto.CategoryIds.Count > 0)
        {
            var found = await _categories.GetByFiltered(c => !c.IsDeleted && dto.CategoryIds.Contains(c.Id), IsTracking: false)
                                         .Select(c => c.Id).ToListAsync();
            if (dto.CategoryIds.Except(found).Any())
                return new("Some CategoryIds were not found", HttpStatusCode.BadRequest);
        }

        if (dto.SubscriptionPlanIds.Count > 0)
        {
            var found = await _subs.GetByFiltered(s => !s.IsDeleted && dto.SubscriptionPlanIds.Contains(s.Id), IsTracking: false)
                                   .Select(s => s.Id).ToListAsync();
            if (dto.SubscriptionPlanIds.Except(found).Any())
                return new("Some SubscriptionPlanIds were not found", HttpStatusCode.BadRequest);
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

        // ------- sync categories (soft-delete) -------
        var newCatIds = dto.CategoryIds.Distinct().ToHashSet();
        gym.GymCategories ??= new List<GymCategory>();

        var toRemove = gym.GymCategories.Where(gc => !gc.IsDeleted && !newCatIds.Contains(gc.CategoryId)).ToList();
        foreach (var rc in toRemove)
        {
            rc.IsDeleted = true;
            rc.UpdatedAt = DateTime.UtcNow;
        }

        var existingCatIds = gym.GymCategories.Where(gc => !gc.IsDeleted).Select(gc => gc.CategoryId).ToHashSet();
        var idsToAdd = newCatIds.Except(existingCatIds).ToList();
        if (idsToAdd.Count > 0)
        {
            var validCatIds = await _categories.GetByFiltered(c => idsToAdd.Contains(c.Id) && !c.IsDeleted, IsTracking: false)
                                               .Select(c => c.Id).ToListAsync();
            foreach (var cid in validCatIds)
                gym.GymCategories.Add(new GymCategory { GymId = gym.Id, CategoryId = cid });
        }

        // ------- sync subscriptions -------
        var newSubIds = dto.SubscriptionPlanIds.Distinct().ToHashSet();
        gym.AvailableSubscriptions = gym.AvailableSubscriptions.Where(s => newSubIds.Contains(s.Id)).ToList();

        var existingSubIds = gym.AvailableSubscriptions.Select(s => s.Id).ToHashSet();
        var subsToAdd = await _subs.GetByFiltered(s => newSubIds.Except(existingSubIds).Contains(s.Id) && !s.IsDeleted).ToListAsync();
        foreach (var sp in subsToAdd) gym.AvailableSubscriptions.Add(sp);

        _gyms.Update(gym);
        await _gyms.SaveChangeAsync();

        return new("Gym updated", HttpStatusCode.OK);
    }

    // ================= DELETE (SOFT) =================
    public async Task<BaseResponse<string>> DeleteAsync(Guid id)
    {
        var gym = await _gyms.GetByFiltered(x => x.Id == id && !x.IsDeleted).FirstOrDefaultAsync();
        if (gym == null) return new("Gym not found", HttpStatusCode.NotFound);

        gym.IsDeleted = true;
        gym.IsActive = false;
        gym.UpdatedAt = DateTime.UtcNow;

        _gyms.Update(gym);
        await _gyms.SaveChangeAsync();

        return new("Gym deleted (soft)", HttpStatusCode.OK);
    }

    // ================= GET BY ID (details) =================
    public async Task<BaseResponse<GymDetailsDto>> GetByIdAsync(Guid id)
    {
        var gym = await _gyms
            .GetByFiltered(g => g.Id == id && !g.IsDeleted,
                           include: new[] { (System.Linq.Expressions.Expression<Func<Gym, object>>)(g => g.District), g => g.GymCategories, g => g.AvailableSubscriptions },
                           IsTracking: false)
            .FirstOrDefaultAsync();

        if (gym == null) return new("Gym not found", null, HttpStatusCode.NotFound);

        var categoryNames = gym.GymCategories?
            .Where(gc => !gc.IsDeleted && !gc.Category.IsDeleted)
            .Select(gc => gc.Category.Name)
            .ToList() ?? new();

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
            Subscriptions = gym.AvailableSubscriptions?.Select(s => s.Name).ToList() ?? new()
        };

        return new("Gym retrieved", dto, HttpStatusCode.OK);
    }

    // ================= GET ALL (paged) =================
    public async Task<BaseResponse<List<GymListItemDto>>> GetAllAsync(int page = 1, int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var query = _gyms
            .GetByFiltered(g => !g.IsDeleted,
                           include: new[] { (System.Linq.Expressions.Expression<Func<Gym, object>>)(g => g.District), g => g.GymCategories, g => g.AvailableSubscriptions },
                           IsTracking: false)
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
                SubscriptionCount = g.AvailableSubscriptions.Count,
                CategoryNames = g.GymCategories.Where(gc => !gc.IsDeleted && !gc.Category.IsDeleted).Select(gc => gc.Category.Name).ToList(),
                SubscriptionNames = g.AvailableSubscriptions.Where(sp => !sp.IsDeleted).Select(sp => sp.Name).ToList()
            })
            .ToListAsync();

        if (list.Count == 0) return new("No gyms found", null, HttpStatusCode.NotFound);

        return new("Gyms retrieved", list, HttpStatusCode.OK);
    }

    // ================= IMAGE: ADD =================
    public async Task<BaseResponse<string>> AddImageAsync(Guid gymId, IFormFile image)
    {
        if (image == null || image.Length == 0)
            return new("Image is required", HttpStatusCode.BadRequest);

        var exists = await _gyms.GetByFiltered(g => g.Id == gymId && !g.IsDeleted, IsTracking: false).AnyAsync();
        if (!exists) return new("Gym not found", HttpStatusCode.NotFound);

        var folder = $"NFIT/gyms/{gymId}";
        var (url, publicId) = await _cloud.UploadImageAsync(image, folder);
        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(publicId))
            return new("Upload failed", HttpStatusCode.BadRequest);

        var img = new Image
        {
            Id = Guid.NewGuid(),
            GymId = gymId,
            ImageUrl = url,
            PublicId = publicId,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        await _images.AddAsync(img);
        await _images.SaveChangeAsync();

        return new("Image added to gym", url, HttpStatusCode.Created);
    }

    // ================= IMAGE: DELETE (hard) =================
    public async Task<BaseResponse<string>> DeleteImageAsync(Guid gymId, Guid imageId)
    {
        var gym = await _gyms.GetByFiltered(g => g.Id == gymId && !g.IsDeleted, include: new[] { (System.Linq.Expressions.Expression<Func<Gym, object>>)(g => g.Images) })
                             .FirstOrDefaultAsync();
        if (gym is null) return new("Gym not found", HttpStatusCode.NotFound);

        var image = gym.Images?.FirstOrDefault(i => i.Id == imageId);
        if (image is null) return new("Image not found", HttpStatusCode.NotFound);

        var cloudDeleted = true;
        if (!string.IsNullOrWhiteSpace(image.PublicId))
            cloudDeleted = await _cloud.DeleteImageAsync(image.PublicId);

        if (!cloudDeleted) return new("Failed to delete from Cloudinary", HttpStatusCode.BadRequest);

        _images.Delete(image);
        await _images.SaveChangeAsync();

        return new("Image hard-deleted", HttpStatusCode.OK);
    }

    // ================= ONLY CATEGORIES ADD =================
    public async Task<BaseResponse<string>> AddCategoriesOnlyAsync(Guid gymId, List<Guid> categoryIds)
    {
        var ids = (categoryIds ?? new()).Where(id => id != Guid.Empty).Distinct().ToList();
        if (ids.Count == 0) return new("No categories provided", HttpStatusCode.BadRequest);

        var gym = await _gyms.GetByFiltered(g => g.Id == gymId && !g.IsDeleted, include: new[] { (System.Linq.Expressions.Expression<Func<Gym, object>>)(g => g.GymCategories) })
                             .FirstOrDefaultAsync();
        if (gym is null) return new("Gym not found", HttpStatusCode.NotFound);

        var living = gym.GymCategories?.Where(gc => !gc.IsDeleted).ToList() ?? new();
        var livingIds = living.Select(gc => gc.CategoryId).ToHashSet();

        var softDeleted = gym.GymCategories?.Where(gc => gc.IsDeleted).ToList() ?? new();
        var softDeletedMap = softDeleted.ToDictionary(x => x.CategoryId, x => x);

        var validIds = await _categories.GetByFiltered(c => ids.Contains(c.Id) && !c.IsDeleted, IsTracking: false)
                                        .Select(c => c.Id).ToListAsync();

        int restored = 0, added = 0, skipped = 0, notFound = ids.Count - validIds.Count;

        foreach (var cid in validIds)
        {
            if (livingIds.Contains(cid)) { skipped++; continue; }
            if (softDeletedMap.TryGetValue(cid, out var gc))
            {
                gc.IsDeleted = false;
                gc.UpdatedAt = DateTime.UtcNow;
                restored++;
            }
            else
            {
                gym.GymCategories!.Add(new GymCategory { GymId = gym.Id, CategoryId = cid });
                added++;
            }
        }

        _gyms.Update(gym);
        await _gyms.SaveChangeAsync();

        var msg = $"Categories => added:{added}, restored:{restored}, skipped_existing:{skipped}, not_found:{notFound}";
        return new(msg, HttpStatusCode.OK);
    }

    // ================= ONLY SUBSCRIPTIONS ADD =================
    public async Task<BaseResponse<string>> AddSubscriptionsOnlyAsync(Guid gymId, List<Guid> subscriptionIds)
    {
        var ids = (subscriptionIds ?? new()).Where(id => id != Guid.Empty).Distinct().ToList();
        if (ids.Count == 0) return new("No subscriptions provided", HttpStatusCode.BadRequest);

        var gym = await _gyms.GetByFiltered(g => g.Id == gymId && !g.IsDeleted, include: new[] { (System.Linq.Expressions.Expression<Func<Gym, object>>)(g => g.AvailableSubscriptions) })
                             .FirstOrDefaultAsync();
        if (gym is null) return new("Gym not found", HttpStatusCode.NotFound);

        var existing = gym.AvailableSubscriptions?.Select(s => s.Id).ToHashSet() ?? new();

        var toAdd = await _subs.GetByFiltered(s => ids.Contains(s.Id) && !s.IsDeleted && !existing.Contains(s.Id)).ToListAsync();
        foreach (var sp in toAdd) gym.AvailableSubscriptions!.Add(sp);

        _gyms.Update(gym);
        await _gyms.SaveChangeAsync();

        var msg = $"Subscriptions => added:{toAdd.Count}, skipped_existing_or_notfound:{ids.Count - toAdd.Count}";
        return new(msg, HttpStatusCode.OK);
    }
}
