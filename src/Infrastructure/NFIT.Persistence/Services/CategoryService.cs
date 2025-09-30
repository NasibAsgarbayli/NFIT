using System.Linq.Expressions;
using System.Net;
using Microsoft.EntityFrameworkCore;
using NFIT.Application.Abstracts.Repositories;
using NFIT.Application.Abstracts.Services;
using NFIT.Application.DTOs.CategoryDtos;
using NFIT.Application.DTOs.GymDtos;
using NFIT.Application.Shared;
using NFIT.Domain.Entities;
using NFIT.Persistence.Contexts;

namespace NFIT.Persistence.Services;

public class CategoryService:ICategoryService
{
    private readonly ICategoryRepository _repo;

    public CategoryService(ICategoryRepository repo)
    {
        _repo = repo;
    }

    public async Task<BaseResponse<Guid>> CreateAsync(CategoryCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return new BaseResponse<Guid>("Name is required", Guid.Empty, HttpStatusCode.BadRequest);

        var name = dto.Name.Trim();
        if (name.Length > 100)
            return new BaseResponse<Guid>("Name is too long (max 100)", Guid.Empty, HttpStatusCode.BadRequest);

        // AnyAsync əlavə etmədən: GetAllFiltered(...).AnyAsync()
        var exists = await _repo
            .GetAllFiltered(
                predicate: c => !c.IsDeleted && c.Name.ToUpper() == name.ToUpper(),
                IsTracking: false
            )
            .AnyAsync();

        if (exists)
            return new BaseResponse<Guid>("Category with this name already exists", Guid.Empty, HttpStatusCode.Conflict);

        var entity = new Category
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = (dto.Description ?? "").Trim()
        };

        await _repo.AddAsync(entity);
        await _repo.SaveChangeAsync();

        return new BaseResponse<Guid>("Category created", entity.Id, HttpStatusCode.Created);
    }

    public async Task<BaseResponse<string>> UpdateAsync(CategoryUpdateDto dto)
    {
        // Tracking lazım olduğuna görə IsTracking: true
        var category = await _repo
            .GetAllFiltered(
                predicate: c => c.Id == dto.Id && !c.IsDeleted,
                IsTracking: true
            )
            .FirstOrDefaultAsync();

        if (category is null)
            return new BaseResponse<string>("Category not found", HttpStatusCode.NotFound);

        if (string.IsNullOrWhiteSpace(dto.Name))
            return new BaseResponse<string>("Name is required", HttpStatusCode.BadRequest);

        var name = dto.Name.Trim();
        if (name.Length > 100)
            return new BaseResponse<string>("Name is too long (max 100)", HttpStatusCode.BadRequest);

        var exists = await _repo
            .GetAllFiltered(
                predicate: c => !c.IsDeleted && c.Id != dto.Id && c.Name.ToUpper() == name.ToUpper(),
                IsTracking: false
            )
            .AnyAsync();

        if (exists)
            return new BaseResponse<string>("Category with this name already exists", HttpStatusCode.Conflict);

        category.Name = name;
        category.Description = (dto.Description ?? "").Trim();
        category.UpdatedAt = DateTime.UtcNow;

        _repo.Update(category);
        await _repo.SaveChangeAsync();

        return new BaseResponse<string>("Category updated", HttpStatusCode.OK);
    }

    public async Task<BaseResponse<string>> DeleteAsync(Guid id)
    {
        // Gyms lazım ola bilər deyə include veririk; tracking də lazımdır (soft delete)
        Expression<Func<Category, object>>[] includes = { c => c.Gyms };

        var category = await _repo
            .GetAllFiltered(
                predicate: c => c.Id == id && !c.IsDeleted,
                include: includes,
                IsTracking: true
            )
            .FirstOrDefaultAsync();

        if (category is null)
            return new BaseResponse<string>("Category not found", HttpStatusCode.NotFound);

        category.IsDeleted = true;
        category.UpdatedAt = DateTime.UtcNow;

        _repo.Update(category);
        await _repo.SaveChangeAsync();

        return new BaseResponse<string>("Category deleted (soft)", HttpStatusCode.OK);
    }

    public async Task<BaseResponse<CategoryGetDto>> GetByIdAsync(Guid id)
    {
        Expression<Func<Category, object>>[] includes = { c => c.Gyms };

        var category = await _repo
            .GetAllFiltered(
                predicate: c => c.Id == id && !c.IsDeleted,
                include: includes,
                IsTracking: false
            )
            .FirstOrDefaultAsync();

        if (category is null)
            return new BaseResponse<CategoryGetDto>("Category not found", null, HttpStatusCode.NotFound);

        var dto = new CategoryGetDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            GymCount = category.Gyms?.Count(g => !g.IsDeleted) ?? 0
        };

        return new BaseResponse<CategoryGetDto>("Category retrieved", dto, HttpStatusCode.OK);
    }

    public async Task<BaseResponse<List<CategoryGetDto>>> GetAllAsync()
    {
        Expression<Func<Category, object>>[] includes = { c => c.Gyms };

        var list = await _repo
            .GetAllFiltered(
                predicate: c => !c.IsDeleted,
                include: includes,
                orderBy: c => c.Name,
                IsOrderByAsc: true,
                IsTracking: false
            )
            .Select(c => new CategoryGetDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                GymCount = c.Gyms.Count(g => !g.IsDeleted)
            })
            .ToListAsync();

        if (list.Count == 0)
            return new BaseResponse<List<CategoryGetDto>>("No categories found", null, HttpStatusCode.NotFound);

        return new BaseResponse<List<CategoryGetDto>>("Categories retrieved", list, HttpStatusCode.OK);
    }

    public async Task<BaseResponse<CategoryGetDto>> GetByNameAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return new BaseResponse<CategoryGetDto>("Name is required", null, HttpStatusCode.BadRequest);

        Expression<Func<Category, object>>[] includes = { c => c.Gyms };

        var category = await _repo
            .GetAllFiltered(
                predicate: c => !c.IsDeleted && c.Name.ToUpper() == name.Trim().ToUpper(),
                include: includes,
                IsTracking: false
            )
            .FirstOrDefaultAsync();

        if (category is null)
            return new BaseResponse<CategoryGetDto>("Category not found", null, HttpStatusCode.NotFound);

        var dto = new CategoryGetDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            GymCount = category.Gyms?.Count(g => !g.IsDeleted) ?? 0
        };

        return new BaseResponse<CategoryGetDto>("Category retrieved", dto, HttpStatusCode.OK);
    }

    public async Task<BaseResponse<CategoryWithGymsDto>> GetByIdWithGymsAsync(Guid id)
    {
        // Sənin include imzası yalnız 1 səviyyə üçündür. Dərin include-lar üçün
        // əvvəlcə Gyms-i daxil edirik, sonra ThenInclude-ları servisdə zəncir edirik.
        var query = _repo
            .GetAllFiltered(
                predicate: c => c.Id == id && !c.IsDeleted,
                include: new Expression<Func<Category, object>>[] { c => c.Gyms },
                IsTracking: false
            )
            // Buradan sonra artıq IQueryable<Category> olduğuna görə ThenInclude işləyir:
            .Include(c => c.Gyms).ThenInclude(g => g.District)
            .Include(c => c.Gyms).ThenInclude(g => g.GymCategories).ThenInclude(gc => gc.Category)
            .Include(c => c.Gyms).ThenInclude(g => g.AvailableSubscriptions);

        var category = await query.FirstOrDefaultAsync();

        if (category is null)
            return new BaseResponse<CategoryWithGymsDto>("Category not found", null, HttpStatusCode.NotFound);

        var activeGyms = (category.Gyms ?? new List<Gym>()).Where(g => !g.IsDeleted).ToList();

        var dto = new CategoryWithGymsDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            GymCount = activeGyms.Count,
            Gyms = activeGyms
                .OrderByDescending(g => g.IsPremium)
                .ThenBy(g => g.Name)
                .Select(g => new GymListItemDto
                {
                    Id = g.Id,
                    Name = g.Name ?? "",
                    Address = g.Address ?? "",
                    DistrictName = g.District?.Name ?? "",
                    IsPremium = g.IsPremium,
                    IsActive = g.IsActive,
                    Rating = g.Rating,
                    CategoryCount = g.GymCategories?.Count(gc => !gc.IsDeleted) ?? 0,
                    SubscriptionCount = g.AvailableSubscriptions?.Count(a => !a.IsDeleted) ?? 0,
                    CategoryNames = (g.GymCategories ?? new List<GymCategory>())
                        .Where(gc => !gc.IsDeleted && gc.Category != null && !gc.Category.IsDeleted)
                        .Select(gc => gc.Category!.Name)
                        .Distinct()
                        .ToList()
                })
                .ToList()
        };

        var message = dto.GymCount == 0 ? "Bu kateqoriyaya aid gym yoxdur" : "Category with gyms retrieved";
        return new BaseResponse<CategoryWithGymsDto>(message, dto, HttpStatusCode.OK);
    }

    public async Task<BaseResponse<int>> GetCategoryCountAsync()
    {
        var count = await _repo
            .GetAllFiltered(predicate: c => !c.IsDeleted, IsTracking: false)
            .CountAsync();

        return new BaseResponse<int>("Category count retrieved", count, HttpStatusCode.OK);
    }
}

