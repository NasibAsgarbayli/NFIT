using System.Linq.Expressions;
using System.Net;
using Microsoft.EntityFrameworkCore;
using NFIT.Application.Abstracts.Repositories;
using NFIT.Application.Abstracts.Services;
using NFIT.Application.DTOs.DistrictDtos;
using NFIT.Application.Shared;
using NFIT.Domain.Entities;
using NFIT.Persistence.Contexts;

namespace NFIT.Persistence.Services;

public class DistrictService:IDistrictService
{
    private readonly IDistrictRepository _repo;
    private readonly IGymRepository _gymRepo;

    public DistrictService(IDistrictRepository repo, IGymRepository gymRepo)
    {
        _repo = repo;
        _gymRepo = gymRepo;
    }

    // CREATE
    public async Task<BaseResponse<Guid>> CreateAsync(DistrictCreateDto dto)
    {
        var name = dto.Name.Trim();

        var exists = await _repo
            .GetAllFiltered(d => !d.IsDeleted && d.Name.ToUpper() == name.ToUpper())
            .AnyAsync();

        if (exists)
            return new BaseResponse<Guid>("District name already exists", Guid.Empty, HttpStatusCode.Conflict);

        var entity = new District
        {
            Id = Guid.NewGuid(),
            Name = name,
            IsActive = true
        };

        await _repo.AddAsync(entity);
        await _repo.SaveChangeAsync();

        return new BaseResponse<Guid>("District created", entity.Id, HttpStatusCode.Created);
    }

    // UPDATE
    public async Task<BaseResponse<string>> UpdateAsync(DistrictUpdateDto dto)
    {
        var name = dto.Name.Trim();

        var district = await _repo
            .GetAllFiltered(d => d.Id == dto.Id && !d.IsDeleted, IsTracking: true)
            .FirstOrDefaultAsync();

        if (district is null)
            return new BaseResponse<string>("District not found", HttpStatusCode.NotFound);

        var dup = await _repo
            .GetAllFiltered(d => !d.IsDeleted && d.Id != dto.Id && d.Name.ToUpper() == name.ToUpper())
            .AnyAsync();

        if (dup)
            return new BaseResponse<string>("Another district with same name exists", HttpStatusCode.Conflict);

        district.Name = name;
        district.UpdatedAt = DateTime.UtcNow;

        _repo.Update(district);
        await _repo.SaveChangeAsync();

        return new BaseResponse<string>("District updated", HttpStatusCode.OK);
    }

    // SOFT DELETE
    public async Task<BaseResponse<string>> DeleteAsync(Guid id)
    {
        var district = await _repo
            .GetAllFiltered(d => d.Id == id && !d.IsDeleted, IsTracking: true)
            .FirstOrDefaultAsync();

        if (district is null)
            return new BaseResponse<string>("District not found", HttpStatusCode.NotFound);

        district.IsDeleted = true;
        district.IsActive = false;
        district.UpdatedAt = DateTime.UtcNow;

        _repo.Update(district);
        await _repo.SaveChangeAsync();

        return new BaseResponse<string>("District deleted (soft)", HttpStatusCode.OK);
    }

    // DEACTIVATE
    public async Task<BaseResponse<string>> DeactivateDistrictAsync(Guid id)
    {
        var district = await _repo
            .GetAllFiltered(d => d.Id == id && !d.IsDeleted, IsTracking: true)
            .FirstOrDefaultAsync();

        if (district is null)
            return new BaseResponse<string>("District not found", HttpStatusCode.NotFound);

        if (!district.IsActive)
            return new BaseResponse<string>("District already deactivated", HttpStatusCode.OK);

        district.IsActive = false;
        district.UpdatedAt = DateTime.UtcNow;

        _repo.Update(district);
        await _repo.SaveChangeAsync();

        return new BaseResponse<string>("District deactivated", HttpStatusCode.OK);
    }

    // GET ALL
    public async Task<BaseResponse<List<DistrictGetDto>>> GetAllAsync()
    {
        Expression<Func<District, object>>[] includes = { d => d.Gyms };

        var list = await _repo
            .GetAllFiltered(d => !d.IsDeleted && d.IsActive, includes, orderBy: d => d.Name, IsOrderByAsc: true)
            .AsNoTracking()
            .Select(d => new DistrictGetDto
            {
                Id = d.Id,
                Name = d.Name,
                IsActive = d.IsActive,
                City = d.City,
                GymCount = d.Gyms.Count(g => !g.IsDeleted && g.IsActive)
            })
            .ToListAsync();

        if (list.Count == 0)
            return new BaseResponse<List<DistrictGetDto>>("No active districts found", null, HttpStatusCode.NotFound);

        return new BaseResponse<List<DistrictGetDto>>("Active districts retrieved", list, HttpStatusCode.OK);
    }

    // GYM COUNT
    public async Task<BaseResponse<int>> GetGymCountByDistrictAsync(Guid districtId, bool onlyActiveGyms = true)
    {
        var district = await _repo
            .GetAllFiltered(d => d.Id == districtId && !d.IsDeleted)
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (district is null)
            return new BaseResponse<int>("District not found", 0, HttpStatusCode.NotFound);

        if (!district.IsActive)
            return new BaseResponse<int>("District is deactivated and cannot be used", 0, HttpStatusCode.BadRequest);

        var gyms = _gymRepo
            .GetAllFiltered(g => !g.IsDeleted && g.DistrictId == districtId);

        if (onlyActiveGyms)
            gyms = gyms.Where(g => g.IsActive);

        var count = await gyms.CountAsync();
        return new BaseResponse<int>("Gym count retrieved", count, HttpStatusCode.OK);
    }

    // ACTIVATE
    public async Task<BaseResponse<string>> ActivateDistrictAsync(Guid id)
    {
        var district = await _repo
            .GetAllFiltered(d => d.Id == id && !d.IsDeleted, IsTracking: true)
            .FirstOrDefaultAsync();

        if (district is null)
            return new BaseResponse<string>("District not found", HttpStatusCode.NotFound);

        if (district.IsActive)
            return new BaseResponse<string>("District is already active", HttpStatusCode.OK);

        district.IsActive = true;
        district.UpdatedAt = DateTime.UtcNow;

        _repo.Update(district);
        await _repo.SaveChangeAsync();

        return new BaseResponse<string>("District activated", HttpStatusCode.OK);
    }

}
