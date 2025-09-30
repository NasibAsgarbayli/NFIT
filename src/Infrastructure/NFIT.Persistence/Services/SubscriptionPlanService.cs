using System.Net;
using Microsoft.EntityFrameworkCore;
using NFIT.Application.Abstracts.Services;
using NFIT.Application.DTOs.SubscriptionPlanDtos;
using NFIT.Application.Shared;
using NFIT.Domain.Entities;
using NFIT.Domain.Enums;
using NFIT.Persistence.Contexts;

namespace NFIT.Persistence.Services;

public class SubscriptionPlanService : ISubscriptionPlanService
{
    private readonly NFITDbContext _context;
    public SubscriptionPlanService(NFITDbContext context) => _context = context;

    // CREATE
    public async Task<BaseResponse<Guid>> CreateAsync(SubscriptionPlanCreateDto dto)
    {
        if (dto == null)
            return new BaseResponse<Guid>("Body is required", Guid.Empty, HttpStatusCode.BadRequest);

        if (string.IsNullOrWhiteSpace(dto.Name))
            return new BaseResponse<Guid>("Name is required", Guid.Empty, HttpStatusCode.BadRequest);

        var name = dto.Name.Trim();
        if (name.Length < 2 || name.Length > 120)
            return new BaseResponse<Guid>("Name length must be between 2 and 120", Guid.Empty, HttpStatusCode.BadRequest);

        // ✅ Enum yoxlamaları
        if (!Enum.IsDefined(typeof(SubscriptionType), dto.Type))
            return new BaseResponse<Guid>("Invalid plan type", Guid.Empty, HttpStatusCode.BadRequest);

        if (!Enum.IsDefined(typeof(BillingCycle), dto.BillingCycle))
            return new BaseResponse<Guid>("Invalid billing cycle", Guid.Empty, HttpStatusCode.BadRequest);

        // ✅ Qiymət guard
        if (dto.Price < 0)
            return new BaseResponse<Guid>("Price cannot be negative", Guid.Empty, HttpStatusCode.BadRequest);

        // ✅ Unikallıq (Name + Type + BillingCycle)
        var exists = await _context.SubscriptionPlans.AnyAsync(sp =>
            !sp.IsDeleted &&
            sp.Type == dto.Type &&
            sp.BillingCycle == dto.BillingCycle &&
            sp.Name != null &&
            sp.Name.Trim().ToLower() == name.ToLower());

        if (exists)
            return new BaseResponse<Guid>("A subscription plan with same name/type/cycle already exists",
                                          Guid.Empty, HttpStatusCode.Conflict);

        var entity = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = dto.Description?.Trim(),
            Type = dto.Type,
            BillingCycle = dto.BillingCycle,
            Price = dto.Price
        };

        await _context.SubscriptionPlans.AddAsync(entity);
        await _context.SaveChangesAsync();

        return new BaseResponse<Guid>("Subscription plan created", entity.Id, HttpStatusCode.Created);
    }

    // UPDATE
    public async Task<BaseResponse<string>> UpdateAsync(SubscriptionPlanUpdateDto dto)
    {
        if (dto == null)
            return new BaseResponse<string>("Body is required", HttpStatusCode.BadRequest);

        if (string.IsNullOrWhiteSpace(dto.Name))
            return new BaseResponse<string>("Name is required", HttpStatusCode.BadRequest);

        var sp = await _context.SubscriptionPlans
            .Include(x => x.Gyms)
            .FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted);

        if (sp is null)
            return new BaseResponse<string>("Subscription plan not found", HttpStatusCode.NotFound);

        var name = dto.Name.Trim();
        if (name.Length < 2 || name.Length > 120)
            return new BaseResponse<string>("Name length must be between 2 and 120", HttpStatusCode.BadRequest);

        // ✅ Enum yoxlamaları
        if (!Enum.IsDefined(typeof(SubscriptionType), dto.Type))
            return new BaseResponse<string>("Invalid plan type", HttpStatusCode.BadRequest);

        if (!Enum.IsDefined(typeof(BillingCycle), dto.BillingCycle))
            return new BaseResponse<string>("Invalid billing cycle", HttpStatusCode.BadRequest);

        // ✅ Qiymət guard
        if (dto.Price < 0)
            return new BaseResponse<string>("Price cannot be negative", HttpStatusCode.BadRequest);

        // ✅ Unikallıq (özündən başqa)
        var exists = await _context.SubscriptionPlans.AnyAsync(x =>
            !x.IsDeleted &&
            x.Id != dto.Id &&
            x.Type == dto.Type &&
            x.BillingCycle == dto.BillingCycle &&
            x.Name != null &&
            x.Name.Trim().ToLower() == name.ToLower());

        if (exists)
            return new BaseResponse<string>("A subscription plan with same name/type/cycle already exists",
                                            HttpStatusCode.Conflict);

        sp.Name = name;
        sp.Description = dto.Description?.Trim();
        sp.Type = dto.Type;
        sp.BillingCycle = dto.BillingCycle;
        sp.Price = dto.Price;
        sp.UpdatedAt = DateTime.UtcNow;

        _context.SubscriptionPlans.Update(sp);
        await _context.SaveChangesAsync();

        return new BaseResponse<string>("Subscription plan updated", HttpStatusCode.OK);
    }
    // SOFT DELETE
    public async Task<BaseResponse<string>> DeleteAsync(Guid id)
    {
        var sp = await _context.SubscriptionPlans.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (sp is null)
            return new BaseResponse<string>("Subscription plan not found", HttpStatusCode.NotFound);

        sp.IsDeleted = true;
        sp.UpdatedAt = DateTime.UtcNow;

        _context.SubscriptionPlans.Update(sp);
        await _context.SaveChangesAsync();

        return new BaseResponse<string>("Subscription plan deleted (soft)", HttpStatusCode.OK);
    }

    // GET ALL
    public async Task<BaseResponse<List<SubscriptionPlanGetDto>>> GetAllAsync()
    {
        var list = await _context.SubscriptionPlans
            .Include(s => s.Gyms)
            .Where(s => !s.IsDeleted)
            .OrderBy(s => s.Name)
            .Select(s => new SubscriptionPlanGetDto
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                Type = s.Type,
                BillingCycle = s.BillingCycle,
                Price = s.Price,
                GymCount = s.Gyms.Count(g => !g.IsDeleted)
            })
            .ToListAsync();

        if (list.Count == 0)
            return new BaseResponse<List<SubscriptionPlanGetDto>>("No subscription plans found",
                                                                  null, HttpStatusCode.NotFound);

        return new BaseResponse<List<SubscriptionPlanGetDto>>("Subscription plans retrieved", list, HttpStatusCode.OK);
    }

    // GET BY ID
    public async Task<BaseResponse<SubscriptionPlanGetDto>> GetByIdAsync(Guid id)
    {
        var s = await _context.SubscriptionPlans
            .Include(x => x.Gyms)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (s is null)
            return new BaseResponse<SubscriptionPlanGetDto>("Subscription plan not found", null, HttpStatusCode.NotFound);

        var dto = new SubscriptionPlanGetDto
        {
            Id = s.Id,
            Name = s.Name,
            Description = s.Description,
            Type = s.Type,
            BillingCycle = s.BillingCycle,
            Price = s.Price,
            GymCount = s.Gyms?.Count(g => !g.IsDeleted) ?? 0
        };

        return new BaseResponse<SubscriptionPlanGetDto>("Subscription plan retrieved", dto, HttpStatusCode.OK);
    }

    // GET BY NAME (exact match)
    public async Task<BaseResponse<SubscriptionPlanGetDto>> GetByNameAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return new BaseResponse<SubscriptionPlanGetDto>("Name is required", null, HttpStatusCode.BadRequest);

        var s = await _context.SubscriptionPlans
            .Include(x => x.Gyms)
            .FirstOrDefaultAsync(x => !x.IsDeleted && x.Name.ToLower() == name.Trim().ToLower());

        if (s is null)
            return new BaseResponse<SubscriptionPlanGetDto>("Subscription plan not found", null, HttpStatusCode.NotFound);

        var dto = new SubscriptionPlanGetDto
        {
            Id = s.Id,
            Name = s.Name,
            Description = s.Description,
            Type = s.Type,
            BillingCycle = s.BillingCycle,
            Price = s.Price,
            GymCount = s.Gyms?.Count(g => !g.IsDeleted) ?? 0
        };

        return new BaseResponse<SubscriptionPlanGetDto>("Subscription plan retrieved", dto, HttpStatusCode.OK);
    }
    // 1) Ən ucuz plan
    public async Task<BaseResponse<SubscriptionPlanGetDto>> GetCheapestPlanAsync()
    {
        var s = await _context.SubscriptionPlans
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Price)
            .Select(x => new SubscriptionPlanGetDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                Type = x.Type,
                BillingCycle = x.BillingCycle,
                Price = x.Price,
                GymCount = x.Gyms.Count(g => !g.IsDeleted)
            })
            .FirstOrDefaultAsync();

        if (s is null)
            return new BaseResponse<SubscriptionPlanGetDto>("No subscription plans found", null, HttpStatusCode.NotFound);

        return new BaseResponse<SubscriptionPlanGetDto>("Cheapest plan retrieved", s, HttpStatusCode.OK);
    }

    // 2) Ən bahalı plan
    public async Task<BaseResponse<SubscriptionPlanGetDto>> GetMostExpensivePlanAsync()
    {
        var s = await _context.SubscriptionPlans
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.Price)
            .Select(x => new SubscriptionPlanGetDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                Type = x.Type,
                BillingCycle = x.BillingCycle,
                Price = x.Price,
                GymCount = x.Gyms.Count(g => !g.IsDeleted)
            })
            .FirstOrDefaultAsync();

        if (s is null)
            return new BaseResponse<SubscriptionPlanGetDto>("No subscription plans found", null, HttpStatusCode.NotFound);

        return new BaseResponse<SubscriptionPlanGetDto>("Most expensive plan retrieved", s, HttpStatusCode.OK);
    }

    // 3) Gym sayına görə sıralanmış planlar
    public async Task<BaseResponse<List<SubscriptionPlanGetDto>>> GetPlansByGymCountAsync(bool descending = true)
    {
        var query = _context.SubscriptionPlans
            .Where(x => !x.IsDeleted)
            .Select(x => new
            {
                Plan = x,
                GymCount = x.Gyms.Count(g => !g.IsDeleted)
            });

        query = descending
            ? query.OrderByDescending(x => x.GymCount).ThenBy(x => x.Plan.Name)
            : query.OrderBy(x => x.GymCount).ThenBy(x => x.Plan.Name);

        var list = await query
            .Select(x => new SubscriptionPlanGetDto
            {
                Id = x.Plan.Id,
                Name = x.Plan.Name,
                Description = x.Plan.Description,
                Type = x.Plan.Type,
                BillingCycle = x.Plan.BillingCycle,
                Price = x.Plan.Price,
                GymCount = x.GymCount
            })
            .ToListAsync();

        if (list.Count == 0)
            return new BaseResponse<List<SubscriptionPlanGetDto>>("No subscription plans found", null, HttpStatusCode.NotFound);

        return new BaseResponse<List<SubscriptionPlanGetDto>>("Plans ordered by gym count", list, HttpStatusCode.OK);
    }
}
