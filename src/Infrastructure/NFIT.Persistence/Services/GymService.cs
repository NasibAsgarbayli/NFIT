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
    private readonly IEmailService _emailService;

    public GymService(NFITDbContext context, IFileService fileService, IEmailService emailService)
    {
        _context = context;
        _fileService = fileService;
        _emailService = emailService;
    }

    // CREATE
    public async Task<BaseResponse<Guid>> CreateAsync(GymCreateDto dto)
    {
        if (dto == null)
            return new BaseResponse<Guid>("Body is required", Guid.Empty, HttpStatusCode.BadRequest);

        if (string.IsNullOrWhiteSpace(dto.Name))
            return new BaseResponse<Guid>("Name is required", Guid.Empty, HttpStatusCode.BadRequest);

        if (string.IsNullOrWhiteSpace(dto.Address))
            return new BaseResponse<Guid>("Address is required", Guid.Empty, HttpStatusCode.BadRequest);

        var name = dto.Name.Trim();
        var address = dto.Address.Trim();
        if (name.Length < 2 || name.Length > 120)
            return new BaseResponse<Guid>("Name length must be between 2 and 120", Guid.Empty, HttpStatusCode.BadRequest);
        if (address.Length < 5 || address.Length > 200)
            return new BaseResponse<Guid>("Address length must be between 5 and 200", Guid.Empty, HttpStatusCode.BadRequest);

        // Koordinatlar (opsional verilibsə yoxla)
        if (dto.Latitude is < -90 or > 90)
            return new BaseResponse<Guid>("Latitude must be between -90 and 90", Guid.Empty, HttpStatusCode.BadRequest);
        if (dto.Longitude is < -180 or > 180)
            return new BaseResponse<Guid>("Longitude must be between -180 and 180", Guid.Empty, HttpStatusCode.BadRequest);

        // Sadə email / URL (opsional) — çox sərt deyil, minimal
        if (!string.IsNullOrWhiteSpace(dto.Email) && !dto.Email.Contains('@'))
            return new BaseResponse<Guid>("Invalid email", Guid.Empty, HttpStatusCode.BadRequest);

        // District check
        var districtExists = await _context.Districts.AnyAsync(d => d.Id == dto.DistrictId && !d.IsDeleted);
        if (!districtExists)
            return new BaseResponse<Guid>("District not found", Guid.Empty, HttpStatusCode.BadRequest);

        dto.CategoryIds ??= new List<Guid>();
        dto.SubscriptionPlanIds ??= new List<Guid>();

        // Unique (Name + District) — case-insensitive, invariant
        var nameExists = await _context.Gyms.AnyAsync(g =>
            !g.IsDeleted &&
            g.DistrictId == dto.DistrictId &&
            g.Name != null &&
            g.Name.Trim().ToUpper() == name.ToUpper()
        );
        if (nameExists)
            return new BaseResponse<Guid>("Gym with same name already exists in this district", Guid.Empty, HttpStatusCode.Conflict);

        // Validate CategoryIds
        var catIds = dto.CategoryIds.Distinct().ToList();
        if (catIds.Count > 0)
        {
            var found = await _context.Categories
                .Where(c => !c.IsDeleted && catIds.Contains(c.Id))
                .Select(c => c.Id)
                .ToListAsync();

            if (catIds.Except(found).Any())
                return new BaseResponse<Guid>("Some CategoryIds were not found", Guid.Empty, HttpStatusCode.BadRequest);
        }

        // Validate SubscriptionPlanIds
        var subIds = dto.SubscriptionPlanIds.Distinct().ToList();
        if (subIds.Count > 0)
        {
            var found = await _context.SubscriptionPlans
                .Where(s => !s.IsDeleted && subIds.Contains(s.Id))
                .Select(s => s.Id)
                .ToListAsync();

            if (subIds.Except(found).Any())
                return new BaseResponse<Guid>("Some SubscriptionPlanIds were not found", Guid.Empty, HttpStatusCode.BadRequest);
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

        // join: GymCategories
        foreach (var catId in catIds)
            gym.GymCategories.Add(new GymCategory { GymId = id, CategoryId = catId });

        // collection: AvailableSubscriptions (attach existing)
        if (subIds.Count > 0)
        {
            var subs = await _context.SubscriptionPlans
                .Where(s => subIds.Contains(s.Id) && !s.IsDeleted)
                .ToListAsync();

            foreach (var sp in subs)
                gym.AvailableSubscriptions.Add(sp);
        }

        await _context.Gyms.AddAsync(gym);
        await _context.SaveChangesAsync();

        // Notification (best-effort)
        try
        {
            var allEmails = await _context.Users
                .Where(u => u.IsActive && u.EmailConfirmed && u.Email != null)
                .Select(u => u.Email!)
                .ToListAsync();

            if (allEmails.Count > 0)
            {
                var subject = $"Yeni Gym əlavə olundu: {gym.Name}";
                var body = $@"<p><strong>{gym.Name}</strong> artıq NFIT platformasında!</p>
                          <p>Daha ətraflı məlumat üçün tətbiqimizə daxil olun.</p>";
                await _emailService.SendEmailAsync(allEmails, subject, body);
            }
        }
        catch { /* loglama varsa et; istifadəçiyə 201 verdik, email uğursuzluğunu uduruq */ }

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
          g.Name.Trim() == dto.Name.Trim());
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
                SubscriptionCount = g.AvailableSubscriptions.Count,

                CategoryNames = g.GymCategories
                    .Where(gc => !gc.IsDeleted && !gc.Category.IsDeleted)
                    .Select(gc => gc.Category.Name)
                    .ToList(),

                SubscriptionNames = g.AvailableSubscriptions
                    .Where(sp => !sp.IsDeleted)
                    .Select(sp => sp.Name)
                    .ToList()
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

        // 1) Parent mövcuddurmu? (track etmədən yoxla)
        var exists = await _context.Gyms
            .AsNoTracking()
            .AnyAsync(g => g.Id == gymId && !g.IsDeleted);

        if (!exists)
            return new BaseResponse<string>("Gym not found", HttpStatusCode.NotFound);

        // 2) Yüklə (FileService artıq düzəldilib)
        var url = await _fileService.UploadAsync(image);

        // 3) Birbaşa child əlavə et (parent-i track ETMƏ!)
        var img = new Image
        {
            Id = Guid.NewGuid(),
            GymId = gymId,
            ImageUrl = url,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Images.AddAsync(img);   // <-- yalnız child INSERT
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
    public async Task<BaseResponse<string>> AddCategoriesOnlyAsync(Guid gymId, List<Guid> categoryIds)
    {
        var ids = (categoryIds ?? new()).Where(id => id != Guid.Empty).Distinct().ToList();
        if (ids.Count == 0) return new BaseResponse<string>("No categories provided", HttpStatusCode.BadRequest);

        var gym = await _context.Gyms
            .Include(g => g.GymCategories)
            .FirstOrDefaultAsync(g => g.Id == gymId && !g.IsDeleted);

        if (gym is null) return new BaseResponse<string>("Gym not found", HttpStatusCode.NotFound);

        // mövcud (living) və soft-deleted join-ları ayır
        var living = gym.GymCategories?.Where(gc => !gc.IsDeleted).ToList() ?? new();
        var livingIds = living.Select(gc => gc.CategoryId).ToHashSet();

        var softDeleted = gym.GymCategories?.Where(gc => gc.IsDeleted).ToList() ?? new();
        var softDeletedMap = softDeleted.ToDictionary(x => x.CategoryId, x => x);

        // DB-də həqiqətən mövcud və aktiv kateqoriyaları yoxla
        var validIds = await _context.Categories
            .Where(c => ids.Contains(c.Id) && !c.IsDeleted)
            .Select(c => c.Id)
            .ToListAsync();

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

        // parent-i Update ETMƏ — yalnız SaveChanges kifayətdir
        await _context.SaveChangesAsync();

        var msg = $"Categories => added:{added}, restored:{restored}, skipped_existing:{skipped}, not_found:{notFound}";
        return new BaseResponse<string>(msg, HttpStatusCode.OK);
    }

    public async Task<BaseResponse<string>> AddSubscriptionsOnlyAsync(Guid gymId, List<Guid> subscriptionIds)
    {
        var ids = (subscriptionIds ?? new()).Where(id => id != Guid.Empty).Distinct().ToList();
        if (ids.Count == 0) return new BaseResponse<string>("No subscriptions provided", HttpStatusCode.BadRequest);

        var gym = await _context.Gyms
            .Include(g => g.AvailableSubscriptions)
            .FirstOrDefaultAsync(g => g.Id == gymId && !g.IsDeleted);

        if (gym is null) return new BaseResponse<string>("Gym not found", HttpStatusCode.NotFound);

        var existing = gym.AvailableSubscriptions?.Select(s => s.Id).ToHashSet() ?? new();

        // yalnız mövcud və aktiv planlar
        var toAdd = await _context.SubscriptionPlans
            .Where(s => ids.Contains(s.Id) && !s.IsDeleted && !existing.Contains(s.Id))
            .ToListAsync();

        int added = toAdd.Count;
        int skipped = ids.Count - added; // ya mövcud idi, ya tapılmadı/isDeleted

        foreach (var sp in toAdd)
            gym.AvailableSubscriptions!.Add(sp);

        // parent-i Update ETMƏ!
        await _context.SaveChangesAsync();

        var msg = $"Subscriptions => added:{added}, skipped_existing_or_notfound:{skipped}";
        return new BaseResponse<string>(msg, HttpStatusCode.OK);
    }
}
