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
        if (string.IsNullOrWhiteSpace(dto.Name))
            return new BaseResponse<Guid>("Name is required", Guid.Empty, HttpStatusCode.BadRequest);

        var name = dto.Name.Trim();
        if (name.Length > 100)
            return new BaseResponse<Guid>("Name is too long (max 100)", Guid.Empty, HttpStatusCode.BadRequest);

        var exists = await _context.Categories
            .AnyAsync(c => !c.IsDeleted && c.Name.ToLower() == name.ToLower());
        if (exists)
            return new BaseResponse<Guid>("Category with this name already exists", Guid.Empty, HttpStatusCode.Conflict);

        var entity = new Category
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = (dto.Description ?? "").Trim()
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

        if (string.IsNullOrWhiteSpace(dto.Name))
            return new BaseResponse<string>("Name is required", HttpStatusCode.BadRequest);

        var name = dto.Name.Trim();
        if (name.Length > 100)
            return new BaseResponse<string>("Name is too long (max 100)", HttpStatusCode.BadRequest);

        var exists = await _context.Categories
            .AnyAsync(c => !c.IsDeleted && c.Id != dto.Id && c.Name.ToLower() == name.ToLower());
        if (exists)
            return new BaseResponse<string>("Category with this name already exists", HttpStatusCode.Conflict);

        category.Name = name;
        category.Description = (dto.Description ?? "").Trim();
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
                    .ThenInclude(gc => gc.Category) // <-- KATEQORIYA adlarını yığmaq üçün
            .Include(c => c.Gyms)
                .ThenInclude(g => g.AvailableSubscriptions)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

        if (category is null)
            return new BaseResponse<CategoryWithGymsDto>("Category not found", null, HttpStatusCode.NotFound);

        var activeGyms = (category.Gyms ?? new List<Domain.Entities.Gym>())
            .Where(g => !g.IsDeleted)
            .ToList();

        var dto = new CategoryWithGymsDto
        {
            Id = category.Id,
            Name = category.Name,            // baxılan kateqoriya adı
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
                    CategoryNames = (g.GymCategories ?? new List<Domain.Entities.GymCategory>())
                        .Where(gc => !gc.IsDeleted && gc.Category != null && !gc.Category.IsDeleted)
                        .Select(gc => gc.Category!.Name)
                        .Distinct()
                        .ToList()
                })
                .ToList()
        };

        // BOŞ OLAN HAL: data boş gəlir + mesajda açıq yazılır
        var message = dto.GymCount == 0
            ? "Bu kateqoriyaya aid gym yoxdur"
            : "Category with gyms retrieved";

        return new BaseResponse<CategoryWithGymsDto>(message, dto, HttpStatusCode.OK);
    }

    // 🔽 3) Category sayını gətir
    public async Task<BaseResponse<int>> GetCategoryCountAsync()
    {
        var count = await _context.Categories.CountAsync(c => !c.IsDeleted);
        return new BaseResponse<int>("Category count retrieved", count, HttpStatusCode.OK);
    }
}

