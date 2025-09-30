using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NFIT.Domain.Entities;
using NFIT.Persistence.Configurations;

namespace NFIT.Persistence.Contexts;

public class NFITDbContext:IdentityDbContext<AppUser>
{
    public NFITDbContext(DbContextOptions<NFITDbContext> options) : base(options)
    {
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CategoryConfiguration).Assembly);
        base.OnModelCreating(modelBuilder);

    }
    public DbSet<Category> Categories { get; set; }
    public DbSet<District> Districts { get; set; }
    public DbSet<Exercise> Exercises { get; set; }
    public DbSet<Favourite> Favourites { get; set; }
    public DbSet<Gym> Gyms { get; set; }
    public DbSet<GymCategory> GymCategories { get; set; }
    public DbSet<GymCheckIn> GymCheckIns { get; set; }
    public DbSet<GymQRCode> GymQRCodes { get; set; }
    public DbSet<Image> Images { get; set; }
    public DbSet<Membership> Memberships { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderSupplement> OrderSupplements { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
    public DbSet<Supplement> Supplements { get; set; }
    public DbSet<Trainer> Trainers { get; set; }
    public DbSet<TrainerVideo> TrainerVideos { get; set; }
    public DbSet<TrainerWorkout> TrainerWorkouts { get; set; }
    public DbSet<TrainerWorkoutExercise> TrainerWorkoutExercises { get; set; }
    public DbSet<Workout> Workouts { get; set; }
    public DbSet<WorkoutExercise> WorkoutExercises { get; set; }

}
