using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NFIT.Domain.Entities;

namespace NFIT.Persistence.Configurations;

public class WorkoutExerciseConfiguration : IEntityTypeConfiguration<WorkoutExercise>
{
    public void Configure(EntityTypeBuilder<WorkoutExercise> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Sets)
            .IsRequired();

        builder.Property(x => x.Reps)
            .IsRequired();

        builder.Property(x => x.Duration)
            .IsRequired(false);

        builder.Property(x => x.RestTimeSeconds)
            .IsRequired();

        // Relationship: WorkoutExercise → Workout
        builder.HasOne(x => x.Workout)
            .WithMany(x => x.WorkoutExercises)
            .HasForeignKey(x => x.WorkoutId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relationship: WorkoutExercise → Exercise
        builder.HasOne(x => x.Exercise)
            .WithMany(x => x.WorkoutExercises)
            .HasForeignKey(x => x.ExerciseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
