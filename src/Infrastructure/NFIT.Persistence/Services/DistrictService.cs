using System.Net;
using Microsoft.EntityFrameworkCore;
using NFIT.Application.Abstracts.Services;
using NFIT.Application.DTOs.DistrictDtos;
using NFIT.Application.Shared;
using NFIT.Domain.Entities;
using NFIT.Persistence.Contexts;

namespace NFIT.Persistence.Services;

public class DistrictService:IDistrictService
{
    private readonly NFITDbContext _context;
    public DistrictService(NFITDbContext context)
    {
        _context = context;
    }
    // CREATE
    public async Task<BaseResponse<Guid>> CreateAsync(DistrictCreateDto dto)
    {
        var name = dto.Name.Trim();

        var exists = await _context.Districts
            .AnyAsync(d => !d.IsDeleted && d.Name.ToLower() == name.ToLower());

        if (exists)
            return new BaseResponse<Guid>("District name already exists", Guid.Empty, HttpStatusCode.Conflict);

        var entity = new District
        {
            Id = Guid.NewGuid(),
            Name = name,
            IsActive = true
        };

        await _context.Districts.AddAsync(entity);
        await _context.SaveChangesAsync();

        return new BaseResponse<Guid>("District created", entity.Id, HttpStatusCode.Created);
    }

    // UPDATE
    public async Task<BaseResponse<string>> UpdateAsync(DistrictUpdateDto dto)
    {
        var name = dto.Name.Trim();

        var district = await _context.Districts
            .FirstOrDefaultAsync(d => d.Id == dto.Id && !d.IsDeleted);

        if (district is null)
            return new BaseResponse<string>("District not found", HttpStatusCode.NotFound);

        var dup = await _context.Districts.AnyAsync(d =>
            !d.IsDeleted &&
            d.Id != dto.Id &&
            d.Name.ToLower() == name.ToLower());

        if (dup)
            return new BaseResponse<string>("Another district with same name exists", HttpStatusCode.Conflict);

        district.Name = name;
        district.UpdatedAt = DateTime.UtcNow;

        _context.Districts.Update(district);
        await _context.SaveChangesAsync();

        return new BaseResponse<string>("District updated", HttpStatusCode.OK);
    }

    // SOFT DELETE (+ deactivate)
    public async Task<BaseResponse<string>> DeleteAsync(Guid id)
    {
        var district = await _context.Districts
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

        if (district is null)
            return new BaseResponse<string>("District not found", HttpStatusCode.NotFound);

        district.IsDeleted = true;
        district.IsActive = false;
        district.UpdatedAt = DateTime.UtcNow;

        _context.Districts.Update(district);
        await _context.SaveChangesAsync();

        return new BaseResponse<string>("District deleted (soft)", HttpStatusCode.OK);
    }

    // DEACTIVATE (visible deyil, seçilə bilməz)
    public async Task<BaseResponse<string>> DeactivateDistrictAsync(Guid id)
    {
        var district = await _context.Districts
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

        if (district is null)
            return new BaseResponse<string>("District not found", HttpStatusCode.NotFound);

        if (!district.IsActive)
            return new BaseResponse<string>("District already deactivated", HttpStatusCode.OK);

        district.IsActive = false;
        district.UpdatedAt = DateTime.UtcNow;

        _context.Districts.Update(district);
        await _context.SaveChangesAsync();

        return new BaseResponse<string>("District deactivated", HttpStatusCode.OK);
    }

    // GET ALL
    public async Task<BaseResponse<List<DistrictGetDto>>> GetAllAsync()
    {
        var list = await _context.Districts
            .Include(d => d.Gyms)
            .Where(d => !d.IsDeleted && d.IsActive)   // 👈 yalnız aktivlər
            .OrderBy(d => d.Name)
            .AsNoTracking()
            .Select(d => new DistrictGetDto
            {
                Id = d.Id,
                Name = d.Name,
                IsActive = d.IsActive,  // dəyər həmişə true olacaq
                City = d.City,
                GymCount = d.Gyms.Count(g => !g.IsDeleted && g.IsActive)
            })
            .ToListAsync();

        if (list.Count == 0)
            return new BaseResponse<List<DistrictGetDto>>("No active districts found", null, HttpStatusCode.NotFound);

        return new BaseResponse<List<DistrictGetDto>>("Active districts retrieved", list, HttpStatusCode.OK);
    }

    // GYM COUNT (opsional: yalnız aktiv gym-lər)
    public async Task<BaseResponse<int>> GetGymCountByDistrictAsync(Guid districtId, bool onlyActiveGyms = true)
    {
        var exists = await _context.Districts.AnyAsync(d => d.Id == districtId && !d.IsDeleted);
        if (!exists)
            return new BaseResponse<int>("District not found", 0, HttpStatusCode.NotFound);

        var gyms = _context.Gyms.Where(g => !g.IsDeleted && g.DistrictId == districtId);
        if (onlyActiveGyms) gyms = gyms.Where(g => g.IsActive);

        var count = await gyms.CountAsync();
        return new BaseResponse<int>("Gym count retrieved", count, HttpStatusCode.OK);
    }
}
