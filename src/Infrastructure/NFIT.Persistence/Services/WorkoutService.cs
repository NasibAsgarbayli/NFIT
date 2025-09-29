using System.Net;
using Microsoft.EntityFrameworkCore;
using NFIT.Application.Abstracts.Services;
using NFIT.Application.DTOs.WorkoutDtos;
using NFIT.Application.Shared;
using NFIT.Domain.Entities;
using NFIT.Domain.Enums;
using NFIT.Persistence.Contexts;

namespace NFIT.Persistence.Services;

public class WorkoutService : IWorkoutService
{
    private readonly NFITDbContext _context;
    public WorkoutService(NFITDbContext context)
    {
        _context = context; 
    }
    // CREATE
    public async Task<BaseResponse<Guid>> CreateAsync(WorkoutCreateDto dto)
    {
        var name = dto.Name.Trim();
        var dup = await _context.Workouts
            .AnyAsync(x => !x.IsDeleted && x.Name.ToLower() == name.ToLower());
        if (dup)
            return new BaseResponse<Guid>("Workout with same name exists", Guid.Empty, HttpStatusCode.Conflict);

        // ===== Enum yoxlamaları =====
        if (!Enum.IsDefined(typeof(DifficultyLevel), dto.Difficulty))
            return new BaseResponse<Guid>("Invalid difficulty level", Guid.Empty, HttpStatusCode.BadRequest);

        if (!Enum.IsDefined(typeof(WorkoutCategory), dto.Category))
            return new BaseResponse<Guid>("Invalid workout category", Guid.Empty, HttpStatusCode.BadRequest);

        if (dto.TargetMuscles is not null)
        {
            foreach (var muscle in dto.TargetMuscles)
            {
                if (!Enum.IsDefined(typeof(MuscleGroup), muscle))
                    return new BaseResponse<Guid>("Invalid muscle group value", Guid.Empty, HttpStatusCode.BadRequest);
            }
        }

        // exercises yoxlama
        var validExercises = await _context.Exercises
            .Where(e => !e.IsDeleted && dto.Exercises.Select(x => x.ExerciseId).Contains(e.Id))
            .Select(e => e.Id)
            .ToListAsync();

        if (dto.Exercises.Any(x => !validExercises.Contains(x.ExerciseId)))
            return new BaseResponse<Guid>("Some exercises not found", Guid.Empty, HttpStatusCode.BadRequest);

        // ===== Workout yarat =====
        var id = Guid.NewGuid();
        var workout = new Workout
        {
            Id = id,
            Name = name,
            Description = dto.Description?.Trim() ?? "",
            EstimatedDuration = dto.EstimatedDuration,
            Difficulty = dto.Difficulty,
            Category = dto.Category,
            TargetMuscles = dto.TargetMuscles,
            RequiredEquipment = dto.RequiredEquipment,
            VideoUrl = dto.VideoUrl?.Trim(),
            IsPublic = dto.IsPublic,
            WorkoutExercises = dto.Exercises.Select(x => new WorkoutExercise
            {
                Id = Guid.NewGuid(),
                WorkoutId = id,
                ExerciseId = x.ExerciseId,
                Sets = x.Sets,
                Reps = x.Reps,
                Duration = x.Duration,
                RestTimeSeconds = x.RestTimeSeconds
            }).ToList()
        };

        await _context.Workouts.AddAsync(workout);
        await _context.SaveChangesAsync();
        return new BaseResponse<Guid>("Workout created", id, HttpStatusCode.Created);
    }

    // UPDATE
    public async Task<BaseResponse<string>> UpdateAsync(WorkoutUpdateDto dto)
    {
        // --- Guard-lar (ad + boş exercises) ---
        if (string.IsNullOrWhiteSpace(dto.Name))
            return new BaseResponse<string>("Name is required", HttpStatusCode.BadRequest);

        if (dto.Exercises is null || dto.Exercises.Count == 0)
            return new BaseResponse<string>("At least one exercise is required", HttpStatusCode.BadRequest);

        var w = await _context.Workouts
            .Include(x => x.WorkoutExercises)
            .FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted);

        if (w == null)
            return new BaseResponse<string>("Workout not found", HttpStatusCode.NotFound);

        // Ad unikallığı (özündən başqa)
        var name = dto.Name.Trim();
        var dup = await _context.Workouts.AnyAsync(x =>
            !x.IsDeleted && x.Id != dto.Id && x.Name.ToLower() == name.ToLower());
        if (dup)
            return new BaseResponse<string>("Another workout with same name exists", HttpStatusCode.Conflict);

        // ===== Enum yoxlamaları =====
        if (!Enum.IsDefined(typeof(DifficultyLevel), dto.Difficulty))
            return new BaseResponse<string>("Invalid difficulty level", HttpStatusCode.BadRequest);

        if (!Enum.IsDefined(typeof(WorkoutCategory), dto.Category))
            return new BaseResponse<string>("Invalid workout category", HttpStatusCode.BadRequest);

        if (dto.TargetMuscles is not null)
        {
            foreach (var muscle in dto.TargetMuscles)
            {
                if (!Enum.IsDefined(typeof(MuscleGroup), muscle))
                    return new BaseResponse<string>("Invalid muscle group value", HttpStatusCode.BadRequest);
            }
        }

        // ===== Exercises mövcudluq yoxlaması =====
        var newIds = dto.Exercises.Select(x => x.ExerciseId).ToHashSet();
        var validExercises = await _context.Exercises
            .Where(e => !e.IsDeleted && newIds.Contains(e.Id))
            .Select(e => e.Id)
            .ToListAsync();

        if (newIds.Except(validExercises).Any())
            return new BaseResponse<string>("Some exercises not found", HttpStatusCode.BadRequest);

        // ===== Update sahələri =====
        w.Name = name;
        w.Description = dto.Description?.Trim() ?? "";
        w.EstimatedDuration = dto.EstimatedDuration;
        w.Difficulty = dto.Difficulty;
        w.Category = dto.Category;
        w.TargetMuscles = dto.TargetMuscles;
        w.RequiredEquipment = dto.RequiredEquipment;
        w.VideoUrl = dto.VideoUrl?.Trim();
        w.IsPublic = dto.IsPublic;
        w.UpdatedAt = DateTime.UtcNow;

        // Köhnə exercise-ləri sil, yenilərini yaz
        _context.WorkoutExercises.RemoveRange(w.WorkoutExercises);

        w.WorkoutExercises = dto.Exercises.Select(x => new WorkoutExercise
        {
            Id = Guid.NewGuid(),
            WorkoutId = w.Id,
            ExerciseId = x.ExerciseId,
            Sets = x.Sets,
            Reps = x.Reps,
            Duration = x.Duration,
            RestTimeSeconds = x.RestTimeSeconds
        }).ToList();

        await _context.SaveChangesAsync();
        return new BaseResponse<string>("Workout updated", HttpStatusCode.OK);
    }

    // DELETE (soft)
    public async Task<BaseResponse<string>> DeleteAsync(Guid id)
    {
        var w = await _context.Workouts.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (w == null) return new BaseResponse<string>("Workout not found", HttpStatusCode.NotFound);

        w.IsDeleted = true;
        w.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return new BaseResponse<string>("Workout deleted", HttpStatusCode.OK);
    }

    // GET BY ID
    public async Task<BaseResponse<WorkoutGetDto>> GetByIdAsync(Guid id)
    {
        var w = await _context.Workouts
            .Include(x => x.WorkoutExercises).ThenInclude(we => we.Exercise)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (w == null) return new BaseResponse<WorkoutGetDto>("Workout not found", null, HttpStatusCode.NotFound);

        return new BaseResponse<WorkoutGetDto>("Workout retrieved", Map(w), HttpStatusCode.OK);
    }

    // GET ALL
    public async Task<BaseResponse<List<WorkoutGetDto>>> GetAllAsync()
    {
        var list = await _context.Workouts
            .Include(x => x.WorkoutExercises).ThenInclude(we => we.Exercise)
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Name)
            .AsNoTracking()
            .ToListAsync();

        if (!list.Any())
            return new BaseResponse<List<WorkoutGetDto>>("No workouts found", null, HttpStatusCode.NotFound);

        return new BaseResponse<List<WorkoutGetDto>>("Workouts retrieved", list.Select(Map).ToList(), HttpStatusCode.OK);
    }

    public async Task<BaseResponse<List<WorkoutGetDto>>> GetByCategoryAsync(WorkoutCategory category)
    {
        var list = await _context.Workouts
            .Include(x => x.WorkoutExercises).ThenInclude(we => we.Exercise)
            .Where(x => !x.IsDeleted && x.Category == category)
            .AsNoTracking()
            .ToListAsync();

        if (!list.Any())
            return new BaseResponse<List<WorkoutGetDto>>("No workouts for this category", null, HttpStatusCode.NotFound);

        return new BaseResponse<List<WorkoutGetDto>>("Workouts retrieved", list.Select(Map).ToList(), HttpStatusCode.OK);
    }

    private static WorkoutGetDto Map(Workout w) => new()
    {
        Id = w.Id,
        Name = w.Name,
        Description = w.Description,
        EstimatedDuration = w.EstimatedDuration,
        Difficulty = w.Difficulty,
        Category = w.Category,
        TargetMuscles = w.TargetMuscles,
        RequiredEquipment = w.RequiredEquipment,
        VideoUrl = w.VideoUrl,
        IsPublic = w.IsPublic,
        IsActive = !w.IsDeleted,
        Exercises = w.WorkoutExercises?.Select(e => new WorkoutExerciseDetailDto
        {
            ExerciseId = e.ExerciseId,
            ExerciseName = e.Exercise?.Name ?? "",
            Sets = e.Sets,
            Reps = e.Reps,
            Duration = e.Duration,
            RestTimeSeconds = e.RestTimeSeconds
        }).ToList() ?? new()
    };
}
