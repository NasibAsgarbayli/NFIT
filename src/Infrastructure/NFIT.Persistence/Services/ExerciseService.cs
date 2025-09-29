using System.Net;
using Microsoft.EntityFrameworkCore;
using NFIT.Application.Abstracts.Services;
using NFIT.Application.DTOs.ExerciseDtos;
using NFIT.Application.Shared;
using NFIT.Domain.Entities;
using NFIT.Domain.Enums;
using NFIT.Persistence.Contexts;

namespace NFIT.Persistence.Services;

public class ExerciseService:IExerciseService
{
    private readonly NFITDbContext _context;
    public ExerciseService(NFITDbContext context)
    {
        _context = context;
    }
    // CREATE
    public async Task<BaseResponse<Guid>> CreateAsync(ExerciseCreateDto dto)
    {
        if (dto == null)
            return new BaseResponse<Guid>("Body is required", Guid.Empty, HttpStatusCode.BadRequest);

        if (string.IsNullOrWhiteSpace(dto.Name))
            return new BaseResponse<Guid>("Name is required", Guid.Empty, HttpStatusCode.BadRequest);

        var name = dto.Name.Trim();
        if (name.Length < 2 || name.Length > 100)
            return new BaseResponse<Guid>("Name length must be between 2 and 100", Guid.Empty, HttpStatusCode.BadRequest);

        // enum yoxlamaları
        if (!Enum.IsDefined(typeof(DifficultyLevel), dto.Difficulty))
            return new BaseResponse<Guid>("Invalid difficulty value", Guid.Empty, HttpStatusCode.BadRequest);

        if (!Enum.IsDefined(typeof(MuscleGroup), dto.PrimaryMuscleGroup))
            return new BaseResponse<Guid>("Invalid primary muscle group", Guid.Empty, HttpStatusCode.BadRequest);

        if (dto.SecondaryMuscleGroups != null)
        {
            foreach (var m in dto.SecondaryMuscleGroups)
                if (!Enum.IsDefined(typeof(MuscleGroup), m))
                    return new BaseResponse<Guid>("Invalid value in secondary muscle groups", Guid.Empty, HttpStatusCode.BadRequest);
        }

        var dup = await _context.Exercises
            .AnyAsync(x => !x.IsDeleted && x.Name.ToLower() == name.ToLower());
        if (dup)
            return new BaseResponse<Guid>("Exercise with same name already exists", Guid.Empty, HttpStatusCode.Conflict);

        // secondary-ləri təmizlə (primary ilə üst-üstə düşməsin, dublikat olmasın)
        var cleanedSecondary = dto.SecondaryMuscleGroups?
            .Where(m => m != dto.PrimaryMuscleGroup)
            .Distinct()
            .ToArray();

        var e = new Exercise
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = (dto.Description ?? "").Trim(),
            PrimaryMuscleGroup = dto.PrimaryMuscleGroup,
            SecondaryMuscleGroups = cleanedSecondary,
            Equipment = dto.Equipment,
            VideoUrl = dto.VideoUrl?.Trim(),
            Difficulty = dto.Difficulty,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _context.Exercises.AddAsync(e);
        await _context.SaveChangesAsync();

        return new BaseResponse<Guid>("Exercise created", e.Id, HttpStatusCode.Created);
    }

    // UPDATE
    // UPDATE
    public async Task<BaseResponse<string>> UpdateAsync(ExerciseUpdateDto dto)
    {
        if (dto == null)
            return new BaseResponse<string>("Body is required", HttpStatusCode.BadRequest);

        if (string.IsNullOrWhiteSpace(dto.Name))
            return new BaseResponse<string>("Name is required", HttpStatusCode.BadRequest);

        var e = await _context.Exercises.FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted);
        if (e == null)
            return new BaseResponse<string>("Exercise not found", HttpStatusCode.NotFound);

        var name = dto.Name.Trim();
        if (name.Length < 2 || name.Length > 100)
            return new BaseResponse<string>("Name length must be between 2 and 100", HttpStatusCode.BadRequest);

        // enum yoxlamaları
        if (!Enum.IsDefined(typeof(DifficultyLevel), dto.Difficulty))
            return new BaseResponse<string>("Invalid difficulty value", HttpStatusCode.BadRequest);

        if (!Enum.IsDefined(typeof(MuscleGroup), dto.PrimaryMuscleGroup))
            return new BaseResponse<string>("Invalid primary muscle group", HttpStatusCode.BadRequest);

        if (dto.SecondaryMuscleGroups != null)
        {
            foreach (var m in dto.SecondaryMuscleGroups)
                if (!Enum.IsDefined(typeof(MuscleGroup), m))
                    return new BaseResponse<string>("Invalid value in secondary muscle groups", HttpStatusCode.BadRequest);
        }

        var dup = await _context.Exercises.AnyAsync(x =>
            !x.IsDeleted && x.Id != dto.Id && x.Name.ToLower() == name.ToLower());
        if (dup)
            return new BaseResponse<string>("Another exercise with same name exists", HttpStatusCode.Conflict);

        var cleanedSecondary = dto.SecondaryMuscleGroups?
            .Where(m => m != dto.PrimaryMuscleGroup)
            .Distinct()
            .ToArray();

        e.Name = name;
        e.Description = (dto.Description ?? "").Trim();
        e.PrimaryMuscleGroup = dto.PrimaryMuscleGroup;
        e.SecondaryMuscleGroups = cleanedSecondary;
        e.Equipment = dto.Equipment;
        e.VideoUrl = dto.VideoUrl?.Trim();
        e.Difficulty = dto.Difficulty;
        e.UpdatedAt = DateTime.UtcNow;

        _context.Exercises.Update(e);
        await _context.SaveChangesAsync();

        return new BaseResponse<string>("Exercise updated", HttpStatusCode.OK);
    }
    // DELETE (soft)
    public async Task<BaseResponse<string>> DeleteAsync(Guid id)
    {
        var e = await _context.Exercises.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (e is null)
            return new BaseResponse<string>("Exercise not found", HttpStatusCode.NotFound);

        e.IsDeleted = true;
        e.UpdatedAt = DateTime.UtcNow;

        _context.Exercises.Update(e);
        await _context.SaveChangesAsync();

        return new BaseResponse<string>("Exercise deleted (soft)", HttpStatusCode.OK);
    }

    // GET BY NAME (single best match)
    public async Task<BaseResponse<ExerciseGetDto>> GetByNameAsync(string name)
    {
        var e = await _context.Exercises
            .AsNoTracking()
            .Where(x => !x.IsDeleted && EF.Functions.Like(x.Name, $"%{name.Trim()}%"))
            .OrderBy(x => x.Name)
            .FirstOrDefaultAsync();

        if (e is null)
            return new BaseResponse<ExerciseGetDto>("Exercise not found", null, HttpStatusCode.NotFound);

        return new BaseResponse<ExerciseGetDto>("Exercise retrieved", Map(e), HttpStatusCode.OK);
    }

    // GET BY MUSCLE GROUP (primary OR in secondary)
    public async Task<BaseResponse<List<ExerciseGetDto>>> GetByMuscleGroupAsync(MuscleGroup muscle)
    {
        var list = await _context.Exercises
            .AsNoTracking()
            .Where(x => !x.IsDeleted &&
                        (x.PrimaryMuscleGroup == muscle ||
                         (x.SecondaryMuscleGroups != null && x.SecondaryMuscleGroups.Contains(muscle))))
            .OrderBy(x => x.Name)
            .Select(x => Map(x))
            .ToListAsync();

        if (list.Count == 0)
            return new BaseResponse<List<ExerciseGetDto>>("No exercises for this muscle group", null, HttpStatusCode.NotFound);

        if (!Enum.IsDefined(typeof(MuscleGroup), muscle))
            return new BaseResponse<List<ExerciseGetDto>>("Invalid muscle group", null, HttpStatusCode.BadRequest);

        return new BaseResponse<List<ExerciseGetDto>>("Exercises retrieved", list, HttpStatusCode.OK);
    }

    // map helper
    private static ExerciseGetDto Map(Exercise x) => new()
    {
        Id = x.Id,
        Name = x.Name,
        Description = x.Description,
        PrimaryMuscleGroup = x.PrimaryMuscleGroup,
        SecondaryMuscleGroups = x.SecondaryMuscleGroups,
        Equipment = x.Equipment,
        VideoUrl = x.VideoUrl,
        Difficulty = x.Difficulty,
      
    };

    public async Task<BaseResponse<ExerciseGetDto>> GetByIdAsync(Guid id)
    {
        var e = await _context.Exercises.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (e is null) return new BaseResponse<ExerciseGetDto>("Exercise not found", null, HttpStatusCode.NotFound);

        return new BaseResponse<ExerciseGetDto>("Exercise retrieved", Map(e), HttpStatusCode.OK);
    }
    public async Task<BaseResponse<List<ExerciseGetDto>>> GetAllAsync()
    {
        var list = await _context.Exercises
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Name)
            .Select(x => Map(x))
            .ToListAsync();

        if (list.Count == 0) return new BaseResponse<List<ExerciseGetDto>>("No exercises", null, HttpStatusCode.NotFound);
        return new BaseResponse<List<ExerciseGetDto>>("Exercises retrieved", list, HttpStatusCode.OK);
    }

}
