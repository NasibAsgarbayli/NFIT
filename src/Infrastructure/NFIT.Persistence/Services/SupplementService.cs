using System.Net;
using Microsoft.EntityFrameworkCore;
using NFIT.Application.Abstracts.Services;
using NFIT.Application.DTOs.SupplementDtos;
using NFIT.Application.Shared;
using NFIT.Domain.Entities;
using NFIT.Persistence.Contexts;

namespace NFIT.Persistence.Services;

public class SupplementService:ISupplementService
{
    private readonly NFITDbContext _context;
    private readonly IFileService _fileService;
    public SupplementService(NFITDbContext context, IFileService fileService)
    {
        _context = context;
        _fileService = fileService;
    }
    // CREATE
    public async Task<BaseResponse<Guid>> CreateAsync(SupplementCreateDto dto)
    {
        var name = dto.Name.Trim();

        var dup = await _context.Supplements
            .AnyAsync(s => !s.IsDeleted && s.Name.ToLower() == name.ToLower());

        if (dup)
            return new BaseResponse<Guid>("Supplement name already exists", Guid.Empty, HttpStatusCode.Conflict);

        var entity = new Supplement
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = dto.Description?.Trim() ?? "",
            Price = dto.Price,
            StockQuantity = dto.StockQuantity,
            Brand = dto.Brand?.Trim(),
            Flavor = dto.Flavor?.Trim(),
            Weight = dto.Weight,
            IsActive = true
        };

        await _context.Supplements.AddAsync(entity);
        await _context.SaveChangesAsync();

        return new BaseResponse<Guid>("Supplement created", entity.Id, HttpStatusCode.Created);
    }

    // UPDATE
    public async Task<BaseResponse<string>> UpdateAsync(SupplementUpdateDto dto)
    {
        var entity = await _context.Supplements
            .FirstOrDefaultAsync(s => s.Id == dto.Id && !s.IsDeleted);

        if (entity is null)
            return new BaseResponse<string>("Supplement not found", HttpStatusCode.NotFound);

        var dup = await _context.Supplements.AnyAsync(s =>
            !s.IsDeleted && s.Id != dto.Id && s.Name.ToLower() == dto.Name.Trim().ToLower());

        if (dup)
            return new BaseResponse<string>("Another supplement with same name exists", HttpStatusCode.Conflict);

        entity.Name = dto.Name.Trim();
        entity.Description = dto.Description?.Trim() ?? "";
        entity.Price = dto.Price;
        entity.StockQuantity = dto.StockQuantity;
        entity.Brand = dto.Brand?.Trim();
        entity.Flavor = dto.Flavor?.Trim();
        entity.Weight = dto.Weight;
        entity.IsActive = dto.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        _context.Supplements.Update(entity);
        await _context.SaveChangesAsync();

        return new BaseResponse<string>("Supplement updated", HttpStatusCode.OK);
    }

    // DELETE (soft)
    public async Task<BaseResponse<string>> DeleteAsync(Guid id)
    {
        var entity = await _context.Supplements
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (entity is null)
            return new BaseResponse<string>("Supplement not found", HttpStatusCode.NotFound);

        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.UpdatedAt = DateTime.UtcNow;

        _context.Supplements.Update(entity);
        await _context.SaveChangesAsync();

        return new BaseResponse<string>("Supplement deleted", HttpStatusCode.OK);
    }

    // GET BY ID
    public async Task<BaseResponse<SupplementGetDto>> GetByIdAsync(Guid id)
    {
        var s = await _context.Supplements
            .Include(x => x.Favourites)
            .FirstOrDefaultAsync(su => su.Id == id && !su.IsDeleted);

        if (s is null)
            return new BaseResponse<SupplementGetDto>("Supplement not found", null, HttpStatusCode.NotFound);

        var dto = MapToDto(s);
        return new BaseResponse<SupplementGetDto>("Supplement retrieved", dto, HttpStatusCode.OK);
    }

    // GET ALL (+ brand/search filter)
    public async Task<BaseResponse<List<SupplementGetDto>>> GetAllAsync(SupplementFilterDto? filter = null)
    {
        var q = _context.Supplements
            .Include(s => s.Favourites)
            .Where(s => !s.IsDeleted);

        if (filter != null)
        {
            if (!string.IsNullOrWhiteSpace(filter.Brand))
                q = q.Where(s => s.Brand != null && s.Brand.ToLower() == filter.Brand!.Trim().ToLower());

            if (!string.IsNullOrWhiteSpace(filter.Search))
                q = q.Where(s => EF.Functions.Like(s.Name, $"%{filter.Search!.Trim()}%"));
        }

        // Yalnız aktiv lazımdırsa: q = q.Where(s => s.IsActive);

        var list = await q.AsNoTracking()
            .OrderByDescending(s => s.CreatedAt)
            .Select(MapToDtoExpr)
            .ToListAsync();

        if (list.Count == 0)
            return new BaseResponse<List<SupplementGetDto>>("No supplements found", null, HttpStatusCode.NotFound);

        return new BaseResponse<List<SupplementGetDto>>("Supplements retrieved", list, HttpStatusCode.OK);
    }

    // SEARCH BY NAME (shortcut)
    public async Task<BaseResponse<List<SupplementGetDto>>> SearchByNameAsync(string name)
    {
        var list = await _context.Supplements
            .Include(s => s.Favourites)
            .Where(s => !s.IsDeleted && EF.Functions.Like(s.Name, $"%{name.Trim()}%"))
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .Select(MapToDtoExpr)
            .ToListAsync();

        if (list.Count == 0)
            return new BaseResponse<List<SupplementGetDto>>("No supplements match the search", null, HttpStatusCode.NotFound);

        return new BaseResponse<List<SupplementGetDto>>("Supplements found", list, HttpStatusCode.OK);
    }

    // BY BRAND
    public async Task<BaseResponse<List<SupplementGetDto>>> GetByBrandAsync(string brand)
    {
        var list = await _context.Supplements
            .Include(s => s.Favourites)
            .Where(s => !s.IsDeleted && s.Brand != null && s.Brand.ToLower() == brand.Trim().ToLower())
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .Select(MapToDtoExpr)
            .ToListAsync();

        if (list.Count == 0)
            return new BaseResponse<List<SupplementGetDto>>("No supplements for this brand", null, HttpStatusCode.NotFound);

        return new BaseResponse<List<SupplementGetDto>>("Brand supplements retrieved", list, HttpStatusCode.OK);
    }

    // POPULAR (favorit sayına görə TOP N)
    public async Task<BaseResponse<List<SupplementGetDto>>> GetPopularSupplementsAsync(int top = 10)
    {
        if (top <= 0) top = 10;

        var list = await _context.Supplements
            .Include(s => s.Favourites)
            .Where(s => !s.IsDeleted && s.IsActive)
            .AsNoTracking()
            .OrderByDescending(s => s.Favourites.Count(f => !f.IsDeleted)) // 👈 favorit sayına görə
            .ThenBy(s => s.Name)
            .Take(top)
            .Select(MapToDtoExpr)
            .ToListAsync();

        if (list.Count == 0)
            return new BaseResponse<List<SupplementGetDto>>("No popular supplements found", null, HttpStatusCode.NotFound);

        return new BaseResponse<List<SupplementGetDto>>("Popular supplements retrieved", list, HttpStatusCode.OK);
    }

    // ------ Mapping helpers ------
    private static SupplementGetDto MapToDto(Supplement s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        Description = s.Description,
        Price = s.Price,
        StockQuantity = s.StockQuantity,
        Brand = s.Brand,
        Flavor = s.Flavor,
        Weight = s.Weight,
        IsActive = s.IsActive,
        FavouriteCount = s.Favourites?.Count(f => !f.IsDeleted) ?? 0
    };

    // Expression variant (EF Select üçün)
    private static System.Linq.Expressions.Expression<Func<Supplement, SupplementGetDto>> MapToDtoExpr =>
        s => new SupplementGetDto
        {
            Id = s.Id,
            Name = s.Name,
            Description = s.Description,
            Price = s.Price,
            StockQuantity = s.StockQuantity,
            Brand = s.Brand,
            Flavor = s.Flavor,
            Weight = s.Weight,
            IsActive = s.IsActive,
            FavouriteCount = s.Favourites.Count(f => !f.IsDeleted)
        };
    public async Task<BaseResponse<string>> AddImageAsync(Guid supplementId, SupplementImageUploadDto dto)
    {
        if (dto.File == null || dto.File.Length == 0)
            return new BaseResponse<string>("Image is required", HttpStatusCode.BadRequest);

        var supplement = await _context.Supplements
            .Include(s => s.Images)
            .FirstOrDefaultAsync(s => s.Id == supplementId && !s.IsDeleted);

        if (supplement is null)
            return new BaseResponse<string>("Supplement not found", HttpStatusCode.NotFound);

        var url = await _fileService.UploadAsync(dto.File); // məsələn: /uploads/xyz.jpg

        var img = new Image
        {
            Id = Guid.NewGuid(),
            ImageUrl = url,
            SupplementId = supplementId,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        supplement.Images ??= new List<Image>();
        supplement.Images.Add(img);

        _context.Supplements.Update(supplement);
        await _context.SaveChangesAsync();

        // Data sahəsində URL-i qaytarırıq
        return new BaseResponse<string>("Image added to supplement", url, HttpStatusCode.Created);
    }

    public async Task<BaseResponse<string>> DeleteImageAsync(Guid supplementId, Guid imageId)
    {
        var supplement = await _context.Supplements
            .Include(s => s.Images)
            .FirstOrDefaultAsync(s => s.Id == supplementId && !s.IsDeleted);

        if (supplement is null)
            return new BaseResponse<string>("Supplement not found", HttpStatusCode.NotFound);

        var image = supplement.Images?.FirstOrDefault(i => i.Id == imageId && !i.IsDeleted);
        if (image is null)
            return new BaseResponse<string>("Image not found", HttpStatusCode.NotFound);

        image.IsDeleted = true;
        image.UpdatedAt = DateTime.UtcNow;

        _context.Images.Update(image); // DbSet<Image> olmalıdır
        await _context.SaveChangesAsync();

        return new BaseResponse<string>("Image deleted from supplement", HttpStatusCode.OK);
    }
}
