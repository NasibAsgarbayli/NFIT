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

        // 1) Adla filter (lowercase contains)
        string? name = req.Name?.Trim();
        string? nameLower = string.IsNullOrWhiteSpace(name) ? null : name.ToLower();

        // 2) Adlardan ID-ləri çıxart (Subscription & Category) – OR exact match (case-insensitive)
        var subIdsFromNames = (req.SubscriptionNames?.Count > 0)
            ? await _context.SubscriptionPlans
                .Where(s => !s.IsDeleted && req.SubscriptionNames.Contains(s.Name))
                .Select(s => s.Id)
                .ToListAsync()
            : new List<Guid>();

        var catIdsFromNames = (req.CategoryNames?.Count > 0)
            ? await _context.Categories
                .Where(c => !c.IsDeleted && req.CategoryNames.Contains(c.Name))
                .Select(c => c.Id)
                .ToListAsync()
            : new List<Guid>();

        // 3) Seçilmiş ID-lərin yekunu (IDs ∪ IDsByName)
        var selectedSubIds = req.SubscriptionIds.Concat(subIdsFromNames).Distinct().ToList();
        var selectedCatIds = req.CategoryIds.Concat(catIdsFromNames).Distinct().ToList();

        // 4) Baza sorğusu
        var q = _context.Gyms
            .Include(g => g.District)
            .Include(g => g.GymCategories)
            .Include(g => g.AvailableSubscriptions)
            .Where(g => !g.IsDeleted);

        if (nameLower is not null)
            q = q.Where(g => (g.Name ?? "").ToLower().Contains(nameLower));

        // 5) Subscription filter
        if (selectedSubIds.Count > 0)
        {
            if (req.RequireAllSubscriptions)
            {
                // ALL: seçilənlərin hamısı bu gym-də olmalıdır
                q = q.Where(g =>
                    selectedSubIds.All(selId =>
                        g.AvailableSubscriptions.Any(s => s.Id == selId)));
            }
            else
            {
                // ANY: seçilənlərdən hər hansı biri kifayətdir
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
                        g.GymCategories.Any(gc => gc.CategoryId == selId)));
            }
            else
            {
                q = q.Where(g => g.GymCategories.Any(gc => selectedCatIds.Contains(gc.CategoryId)));
            }
        }

        // 7) Sıra + səhifələmə + yüngül proyeksiya
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
                CategoryCount = g.GymCategories.Count,
                SubscriptionCount = g.AvailableSubscriptions.Count
            })
            .ToListAsync();

        if (list.Count == 0)
            return new BaseResponse<List<GymListItemDto>>("No gyms matched your filters", null, HttpStatusCode.NotFound);

        return new BaseResponse<List<GymListItemDto>>("Gyms retrieved", list, HttpStatusCode.OK);
    }
}
