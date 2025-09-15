using System.Net;
using Microsoft.EntityFrameworkCore;
using NFIT.Application.Abstracts.Services;
using NFIT.Application.DTOs.CategoryDtos;
using NFIT.Application.DTOs.GymDtos;
using NFIT.Application.Shared;
using NFIT.Domain.Entities;
using NFIT.Persistence.Contexts;

namespace NFIT.Persistence.Services;

public class CategoryService:ICategoryService
{
    private readonly NFITDbContext _context;

    public CategoryService(NFITDbContext context)
    {
        _context = context;
    }

    public async Task<BaseResponse<Guid>> CreateAsync(CategoryCreateDto dto)
    {
        // (opsional) eyni adı təkrarlamamaq üçün sadə yoxlama
        var exists = await _context.Categories
            .AnyAsync(c => !c.IsDeleted && c.Name.ToLower() == dto.Name.ToLower());
        if (exists)
            return new BaseResponse<Guid>("Category with this name already exists", Guid.Empty, HttpStatusCode.Conflict);

        var entity = new Category
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim()
        };

        await _context.Categories.AddAsync(entity);
        await _context.SaveChangesAsync();

        return new BaseResponse<Guid>("Category created", entity.Id, HttpStatusCode.Created);
    }

    public async Task<BaseResponse<string>> UpdateAsync(CategoryUpdateDto dto)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == dto.Id && !c.IsDeleted);

        if (category is null)
            return new BaseResponse<string>("Category not found", HttpStatusCode.NotFound);

        // (opsional) ad unikallığı (özündən başqa)
        var exists = await _context.Categories
            .AnyAsync(c => !c.IsDeleted &&
                           c.Id != dto.Id &&
                           c.Name.ToLower() == dto.Name.ToLower());
        if (exists)
            return new BaseResponse<string>("Category with this name already exists", HttpStatusCode.Conflict);

        category.Name = dto.Name.Trim();
        category.Description = dto.Description?.Trim();
        category.UpdatedAt = DateTime.UtcNow;

        _context.Categories.Update(category);
        await _context.SaveChangesAsync();

        return new BaseResponse<string>("Category updated", HttpStatusCode.OK);
    }

    public async Task<BaseResponse<string>> DeleteAsync(Guid id)
    {
        var category = await _context.Categories
            .Include(c => c.Gyms) // əlaqə ilə bağlı qayda yazmaq istəsən gərək ola bilər
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

        if (category is null)
            return new BaseResponse<string>("Category not found", HttpStatusCode.NotFound);

        // ✅ SOFT DELETE
        category.IsDeleted = true;
        category.UpdatedAt = DateTime.UtcNow;

        _context.Categories.Update(category);
        await _context.SaveChangesAsync();

        return new BaseResponse<string>("Category deleted (soft)", HttpStatusCode.OK);
    }

    public async Task<BaseResponse<CategoryGetDto>> GetByIdAsync(Guid id)
    {
        var category = await _context.Categories
            .Include(c => c.Gyms)
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

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
        var categories = await _context.Categories
            .Include(c => c.Gyms)
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.Name)
            .Select(c => new CategoryGetDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                GymCount = c.Gyms.Count(g => !g.IsDeleted)
            })
            .ToListAsync();

        if (categories.Count == 0)
            return new BaseResponse<List<CategoryGetDto>>("No categories found", null, HttpStatusCode.NotFound);

        return new BaseResponse<List<CategoryGetDto>>("Categories retrieved", categories, HttpStatusCode.OK);
    }
    // 🔽 1) Ada görə tap (unikal ad)
    public async Task<BaseResponse<CategoryGetDto>> GetByNameAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return new BaseResponse<CategoryGetDto>("Name is required", null, HttpStatusCode.BadRequest);

        var category = await _context.Categories
            .Include(c => c.Gyms)
            .FirstOrDefaultAsync(c => !c.IsDeleted && c.Name.ToLower() == name.Trim().ToLower());

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

    // 🔽 2) ID ilə category-ni gym-larla birlikdə gətir
    public async Task<BaseResponse<CategoryWithGymsDto>> GetByIdWithGymsAsync(Guid id)
    {
        var category = await _context.Categories
            .Include(c => c.Gyms)
                .ThenInclude(g => g.District)
            .Include(c => c.Gyms)
                .ThenInclude(g => g.GymCategories)
            .Include(c => c.Gyms)
                .ThenInclude(g => g.AvailableSubscriptions)
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

        if (category is null)
            return new BaseResponse<CategoryWithGymsDto>("Category not found", null, HttpStatusCode.NotFound);

        var dto = new CategoryWithGymsDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            GymCount = category.Gyms?.Count(g => !g.IsDeleted) ?? 0,
            Gyms = (category.Gyms ?? new List<Domain.Entities.Gym>())
                .Where(g => !g.IsDeleted)
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
                    CategoryCount = g.GymCategories?.Count ?? 0,
                    SubscriptionCount = g.AvailableSubscriptions?.Count ?? 0
                })
                .ToList()
        };

        return new BaseResponse<CategoryWithGymsDto>("Category with gyms retrieved", dto, HttpStatusCode.OK);
    }

    // 🔽 3) Category sayını gətir
    public async Task<BaseResponse<int>> GetCategoryCountAsync()
    {
        var count = await _context.Categories.CountAsync(c => !c.IsDeleted);
        return new BaseResponse<int>("Category count retrieved", count, HttpStatusCode.OK);
    }
}

