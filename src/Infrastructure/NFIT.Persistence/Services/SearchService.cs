using System.Net;
using Microsoft.EntityFrameworkCore;
using NFIT.Application.Abstracts.Services;
using NFIT.Application.DTOs.GymDtos;
using NFIT.Application.DTOs.SearchDtos;
using NFIT.Application.Shared;
using NFIT.Persistence.Contexts;

namespace NFIT.Persistence.Services;

public class SearchService:ISearchService
{
    private readonly NFITDbContext _context;
    public SearchService(NFITDbContext context)
    {
        _context = context;
    }
    public async Task<BaseResponse<List<GymListItemDto>>> SearchGymsAsync(SearchGymsRequest req)
    {
        // sanitize pagination
        var page = req.Page < 1 ? 1 : req.Page;
        var pageSize = req.PageSize < 1 ? 20 : (req.PageSize > 100 ? 100 : req.PageSize);

        // 1) Name (case-insensitive)
        string? name = req.Name?.Trim();
        string? nameLower = string.IsNullOrWhiteSpace(name) ? null : name.ToLower();

        // 2) Normalize incoming names (case-insensitive)
        var subNameSet = (req.SubscriptionNames ?? new List<string>())
            .Select(s => s?.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!.ToLower())
            .Distinct()
            .ToList();

        var catNameSet = (req.CategoryNames ?? new List<string>())
            .Select(s => s?.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!.ToLower())
            .Distinct()
            .ToList();

        // 2.1) Resolve names -> IDs (case-insensitive exact)
        var subIdsFromNames = (subNameSet.Count > 0)
            ? await _context.SubscriptionPlans
                .Where(s => !s.IsDeleted && s.Name != null && subNameSet.Contains(s.Name.ToLower()))
                .Select(s => s.Id)
                .ToListAsync()
            : new List<Guid>();

        var catIdsFromNames = (catNameSet.Count > 0)
            ? await _context.Categories
                .Where(c => !c.IsDeleted && c.Name != null && catNameSet.Contains(c.Name.ToLower()))
                .Select(c => c.Id)
                .ToListAsync()
            : new List<Guid>();

        // 2.2) STRICT: əgər ad siyahısı verilibsə, amma heç nə tapılmırsa -> NotFound
        if (subNameSet.Count > 0 && subIdsFromNames.Count == 0)
            return new BaseResponse<List<GymListItemDto>>("No gyms matched your subscription names", null, HttpStatusCode.NotFound);

        if (catNameSet.Count > 0 && catIdsFromNames.Count == 0)
            return new BaseResponse<List<GymListItemDto>>("No gyms matched your category names", null, HttpStatusCode.NotFound);

        // 3) Union IDs
        var selectedSubIds = (req.SubscriptionIds ?? Enumerable.Empty<Guid>()).Concat(subIdsFromNames).Distinct().ToList();
        var selectedCatIds = (req.CategoryIds ?? Enumerable.Empty<Guid>()).Concat(catIdsFromNames).Distinct().ToList();

        // 3.1) Sənin istədiyin davranış: heç bir filtr yoxdursa (ad da daxil),
        // nəticə gətirmə (NotFound qaytar)
        var noFilters =
            nameLower is null &&
            selectedSubIds.Count == 0 &&
            selectedCatIds.Count == 0;

        if (noFilters)
            return new BaseResponse<List<GymListItemDto>>("Provide at least one filter (name/category/subscription).", null, HttpStatusCode.NotFound);

        // 4) Base query
        var q = _context.Gyms
            .Include(g => g.District)
            .Include(g => g.GymCategories)
                .ThenInclude(gc => gc.Category)
            .Include(g => g.AvailableSubscriptions)
            .Where(g => !g.IsDeleted);

        if (nameLower is not null)
            q = q.Where(g => (g.Name ?? "").ToLower().Contains(nameLower));

        // 5) Subscription filter
        if (selectedSubIds.Count > 0)
        {
            if (req.RequireAllSubscriptions)
            {
                q = q.Where(g =>
                    selectedSubIds.All(selId =>
                        g.AvailableSubscriptions.Any(s => s.Id == selId)));
            }
            else
            {
                q = q.Where(g => g.AvailableSubscriptions.Any(s => selectedSubIds.Contains(s.Id)));
            }
        }

        // 6) Category filter
        if (selectedCatIds.Count > 0)
        {
            if (req.RequireAllCategories)
            {
                q = q.Where(g =>
                    selectedCatIds.All(selId =>
                        g.GymCategories.Any(gc =>
                            !gc.IsDeleted &&
                            !gc.Category.IsDeleted &&
                            gc.CategoryId == selId)));
            }
            else
            {
                q = q.Where(g =>
                    g.GymCategories.Any(gc =>
                        !gc.IsDeleted &&
                        !gc.Category.IsDeleted &&
                        selectedCatIds.Contains(gc.CategoryId)));
            }
        }

        // 7) Order + paging + projection
        var list = await q.AsNoTracking()
            .OrderByDescending(g => g.IsPremium)
            .ThenBy(g => g.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(g => new GymListItemDto
            {
                Id = g.Id,
                Name = g.Name ?? "",
                Address = g.Address ?? "",
                DistrictName = g.District!.Name,
                IsPremium = g.IsPremium,
                IsActive = g.IsActive,
                Rating = g.Rating,
                CategoryCount = g.GymCategories.Count(gc => !gc.IsDeleted && !gc.Category.IsDeleted),
                SubscriptionCount = g.AvailableSubscriptions.Count
            })
            .ToListAsync();

        if (list.Count == 0)
            return new BaseResponse<List<GymListItemDto>>("No gyms matched your filters", null, HttpStatusCode.NotFound);

        return new BaseResponse<List<GymListItemDto>>("Gyms retrieved", list, HttpStatusCode.OK);
    }
}
