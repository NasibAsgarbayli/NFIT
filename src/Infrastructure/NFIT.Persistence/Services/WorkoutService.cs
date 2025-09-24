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
        var dup = await _context.Workouts.AnyAsync(x => !x.IsDeleted && x.Name.ToLower() == name.ToLower());
        if (dup) return new BaseResponse<Guid>("Workout with same name exists", Guid.Empty, HttpStatusCode.Conflict);

        // exercises yoxlama
        var validExercises = await _context.Exercises
            .Where(e => !e.IsDeleted && dto.Exercises.Select(x => x.ExerciseId).Contains(e.Id))
            .Select(e => e.Id)
            .ToListAsync();
        if (dto.Exercises.Any(x => !validExercises.Contains(x.ExerciseId)))
            return new BaseResponse<Guid>("Some exercises not found", Guid.Empty, HttpStatusCode.BadRequest);

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
        var w = await _context.Workouts
            .Include(x => x.WorkoutExercises)
            .FirstOrDefaultAsync(x => x.Id == dto.Id && !x.IsDeleted);
        if (w == null) return new BaseResponse<string>("Workout not found", HttpStatusCode.NotFound);

        var dup = await _context.Workouts.AnyAsync(x => !x.IsDeleted && x.Id != dto.Id &&
                                                       x.Name.ToLower() == dto.Name.Trim().ToLower());
        if (dup) return new BaseResponse<string>("Another workout with same name exists", HttpStatusCode.Conflict);

        w.Name = dto.Name.Trim();
        w.Description = dto.Description?.Trim() ?? "";
        w.EstimatedDuration = dto.EstimatedDuration;
        w.Difficulty = dto.Difficulty;
        w.Category = dto.Category;
        w.TargetMuscles = dto.TargetMuscles;
        w.RequiredEquipment = dto.RequiredEquipment;
        w.VideoUrl = dto.VideoUrl?.Trim();
        w.IsPublic = dto.IsPublic;
        w.UpdatedAt = DateTime.UtcNow;

        // Exercises yenilə
        var newIds = dto.Exercises.Select(x => x.ExerciseId).ToHashSet();
        var validExercises = await _context.Exercises
            .Where(e => !e.IsDeleted && newIds.Contains(e.Id))
            .Select(e => e.Id)
            .ToListAsync();
        if (newIds.Except(validExercises).Any())
            return new BaseResponse<string>("Some exercises not found", HttpStatusCode.BadRequest);

        // köhnələri sil
        _context.WorkoutExercises.RemoveRange(w.WorkoutExercises);

        // yeniləri əlavə et
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
