using System.Linq.Expressions;
using System.Net;
using Microsoft.EntityFrameworkCore;
using NFIT.Application.Abstracts.Repositories;
using NFIT.Application.Abstracts.Services;
using NFIT.Application.DTOs.WorkoutDtos;
using NFIT.Application.Shared;
using NFIT.Domain.Entities;
using NFIT.Domain.Enums;
using NFIT.Persistence.Contexts;

namespace NFIT.Persistence.Services;

public class WorkoutService : IWorkoutService
{
    private readonly IRepository<Workout> _workoutRepo;
    private readonly IRepository<Exercise> _exerciseRepo;
    private readonly IRepository<WorkoutExercise> _weRepo;

    public WorkoutService(IRepository<Workout> workoutRepo,
                          IRepository<Exercise> exerciseRepo,
                          IRepository<WorkoutExercise> weRepo)
    {
        _workoutRepo = workoutRepo;
        _exerciseRepo = exerciseRepo;
        _weRepo = weRepo;
    }

    // ================== CREATE ==================
    public async Task<BaseResponse<Guid>> CreateAsync(WorkoutCreateDto dto)
    {
        var name = dto.Name.Trim();

        var dup = await _workoutRepo
            .GetByFiltered(x => !x.IsDeleted && x.Name.ToLower() == name.ToLower())
            .AnyAsync();

        if (dup)
            return new BaseResponse<Guid>("Workout with same name exists", Guid.Empty, HttpStatusCode.Conflict);

        // ===== Enum checks =====
        if (!Enum.IsDefined(typeof(DifficultyLevel), dto.Difficulty))
            return new BaseResponse<Guid>("Invalid difficulty level", Guid.Empty, HttpStatusCode.BadRequest);

        if (!Enum.IsDefined(typeof(WorkoutCategory), dto.Category))
            return new BaseResponse<Guid>("Invalid workout category", Guid.Empty, HttpStatusCode.BadRequest);

        if (dto.TargetMuscles is not null)
        {
            foreach (var m in dto.TargetMuscles)
                if (!Enum.IsDefined(typeof(MuscleGroup), m))
                    return new BaseResponse<Guid>("Invalid muscle group value", Guid.Empty, HttpStatusCode.BadRequest);
        }

        // ===== exercises existence =====
        var wantedIds = dto.Exercises.Select(x => x.ExerciseId).ToHashSet();
        var validIds = await _exerciseRepo
            .GetByFiltered(e => !e.IsDeleted && wantedIds.Contains(e.Id))
            .Select(e => e.Id)
            .ToListAsync();

        if (wantedIds.Except(validIds).Any())
            return new BaseResponse<Guid>("Some exercises not found", Guid.Empty, HttpStatusCode.BadRequest);

        // ===== create workout =====
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

        await _workoutRepo.AddAsync(workout);
        await _workoutRepo.SaveChangeAsync();

        return new BaseResponse<Guid>("Workout created", id, HttpStatusCode.Created);
    }

    // ================== UPDATE ==================
    public async Task<BaseResponse<string>> UpdateAsync(WorkoutUpdateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return new BaseResponse<string>("Name is required", HttpStatusCode.BadRequest);

        if (dto.Exercises is null || dto.Exercises.Count == 0)
            return new BaseResponse<string>("At least one exercise is required", HttpStatusCode.BadRequest);

        // load workout + its relations (first-level include)
        var w = await _workoutRepo
            .GetByFiltered(x => x.Id == dto.Id && !x.IsDeleted,
                           include: new Expression<Func<Workout, object>>[] { x => x.WorkoutExercises },
                           IsTracking: true)
            .FirstOrDefaultAsync();

        if (w is null)
            return new BaseResponse<string>("Workout not found", HttpStatusCode.NotFound);

        // name uniqueness (except itself)
        var name = dto.Name.Trim();
        var dup = await _workoutRepo
            .GetByFiltered(x => !x.IsDeleted && x.Id != dto.Id && x.Name.ToLower() == name.ToLower())
            .AnyAsync();

        if (dup)
            return new BaseResponse<string>("Another workout with same name exists", HttpStatusCode.Conflict);

        // enums
        if (!Enum.IsDefined(typeof(DifficultyLevel), dto.Difficulty))
            return new BaseResponse<string>("Invalid difficulty level", HttpStatusCode.BadRequest);

        if (!Enum.IsDefined(typeof(WorkoutCategory), dto.Category))
            return new BaseResponse<string>("Invalid workout category", HttpStatusCode.BadRequest);

        if (dto.TargetMuscles is not null)
        {
            foreach (var m in dto.TargetMuscles)
                if (!Enum.IsDefined(typeof(MuscleGroup), m))
                    return new BaseResponse<string>("Invalid muscle group value", HttpStatusCode.BadRequest);
        }

        // exercises existence
        var newIds = dto.Exercises.Select(x => x.ExerciseId).ToHashSet();
        var validIds = await _exerciseRepo
            .GetByFiltered(e => !e.IsDeleted && newIds.Contains(e.Id))
            .Select(e => e.Id)
            .ToListAsync();

        if (newIds.Except(validIds).Any())
            return new BaseResponse<string>("Some exercises not found", HttpStatusCode.BadRequest);

        // update primitive fields
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

        // remove old WE rows (repo interface-də RemoveRange yoxdur, ona görə loop)
        if (w.WorkoutExercises is not null && w.WorkoutExercises.Count > 0)
        {
            foreach (var old in w.WorkoutExercises.ToList())
                _weRepo.Delete(old);
        }

        // add new WE rows
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

        _workoutRepo.Update(w);
        await _workoutRepo.SaveChangeAsync();

        return new BaseResponse<string>("Workout updated", HttpStatusCode.OK);
    }

    // ================== DELETE (soft) ==================
    public async Task<BaseResponse<string>> DeleteAsync(Guid id)
    {
        var w = await _workoutRepo
            .GetByFiltered(x => x.Id == id && !x.IsDeleted, IsTracking: true)
            .FirstOrDefaultAsync();

        if (w is null)
            return new BaseResponse<string>("Workout not found", HttpStatusCode.NotFound);

        w.IsDeleted = true;
        w.UpdatedAt = DateTime.UtcNow;

        _workoutRepo.Update(w);
        await _workoutRepo.SaveChangeAsync();

        return new BaseResponse<string>("Workout deleted", HttpStatusCode.OK);
    }

    // ================== GET BY ID ==================
    public async Task<BaseResponse<WorkoutGetDto>> GetByIdAsync(Guid id)
    {
        var w = await _workoutRepo
            .GetByFiltered(x => x.Id == id && !x.IsDeleted, IsTracking: false)
            .FirstOrDefaultAsync();

        if (w is null)
            return new BaseResponse<WorkoutGetDto>("Workout not found", null, HttpStatusCode.NotFound);

        // load WorkoutExercises + Exercise
        var wes = await _weRepo
            .GetByFiltered(we => we.WorkoutId == id && !we.IsDeleted,
                           IsTracking: false,
                           include: new Expression<Func<WorkoutExercise, object>>[] { we => we.Exercise })
            .ToListAsync();

        w.WorkoutExercises = wes;

        return new BaseResponse<WorkoutGetDto>("Workout retrieved", Map(w), HttpStatusCode.OK);
    }

    // ================== GET ALL ==================
    public async Task<BaseResponse<List<WorkoutGetDto>>> GetAllAsync()
    {
        var workouts = await _workoutRepo
            .GetAllFiltered(x => !x.IsDeleted,
                            IsTracking: false,
                            orderBy: x => x.Name,
                            IsOrderByAsc: true)
            .ToListAsync();

        if (!workouts.Any())
            return new BaseResponse<List<WorkoutGetDto>>("No workouts found", null, HttpStatusCode.NotFound);

        var ids = workouts.Select(x => x.Id).ToList();

        var wes = await _weRepo
            .GetByFiltered(we => ids.Contains(we.WorkoutId) && !we.IsDeleted,
                           IsTracking: false,
                           include: new Expression<Func<WorkoutExercise, object>>[] { we => we.Exercise })
            .ToListAsync();

        // group relations
        var byWorkout = wes.GroupBy(x => x.WorkoutId).ToDictionary(g => g.Key, g => g.ToList());
        foreach (var w in workouts)
            w.WorkoutExercises = byWorkout.TryGetValue(w.Id, out var list) ? list : new List<WorkoutExercise>();

        return new BaseResponse<List<WorkoutGetDto>>(
            "Workouts retrieved",
            workouts.Select(Map).ToList(),
            HttpStatusCode.OK
        );
    }

    public async Task<BaseResponse<List<WorkoutGetDto>>> GetByCategoryAsync(WorkoutCategory category)
    {
        var workouts = await _workoutRepo
            .GetByFiltered(x => !x.IsDeleted && x.Category == category, IsTracking: false)
            .ToListAsync();

        if (!workouts.Any())
            return new BaseResponse<List<WorkoutGetDto>>("No workouts for this category", null, HttpStatusCode.NotFound);

        var ids = workouts.Select(x => x.Id).ToList();

        var wes = await _weRepo
            .GetByFiltered(we => ids.Contains(we.WorkoutId) && !we.IsDeleted,
                           IsTracking: false,
                           include: new Expression<Func<WorkoutExercise, object>>[] { we => we.Exercise })
            .ToListAsync();

        var byWorkout = wes.GroupBy(x => x.WorkoutId).ToDictionary(g => g.Key, g => g.ToList());
        foreach (var w in workouts)
            w.WorkoutExercises = byWorkout.TryGetValue(w.Id, out var list) ? list : new List<WorkoutExercise>();

        return new BaseResponse<List<WorkoutGetDto>>(
            "Workouts retrieved",
            workouts.Select(Map).ToList(),
            HttpStatusCode.OK
        );
    }

    // ================== Mapper ==================
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
        Exercises = w.WorkoutExercises?
            .Select(e => new WorkoutExerciseDetailDto
            {
                ExerciseId = e.ExerciseId,
                ExerciseName = e.Exercise?.Name ?? "",
                Sets = e.Sets,
                Reps = e.Reps,
                Duration = e.Duration,
                RestTimeSeconds = e.RestTimeSeconds
            }).ToList()
    };
}
