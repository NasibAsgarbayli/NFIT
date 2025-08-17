using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NFIT.Domain.Entities;

namespace NFIT.Persistence.Configurations;

public class TrainerWorkoutExerciseConfiguration : IEntityTypeConfiguration<TrainerWorkoutExercise>
{
    public void Configure(EntityTypeBuilder<TrainerWorkoutExercise> builder)
    {

        builder.ToTable("TrainerWorkoutExercises");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Sets)
            .IsRequired();

        builder.Property(x => x.Reps)
            .IsRequired();

        builder.Property(x => x.Duration)
            .IsRequired(false);

        builder.Property(x => x.RestTimeSeconds)
            .IsRequired();

        builder.Property(x => x.TrainerNotes)
            .HasMaxLength(500);

        builder.Property(x => x.VideoUrl)
            .HasMaxLength(300);

        // Relation: TrainerWorkoutExercise -> TrainerWorkout (Many-to-One)
        builder.HasOne(x => x.TrainerWorkout)
            .WithMany(x => x.WorkoutExercises)
            .HasForeignKey(x => x.TrainerWorkoutId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relation: TrainerWorkoutExercise -> Exercise (Many-to-One)
        builder.HasOne(x => x.Exercise)
            .WithMany()
            .HasForeignKey(x => x.ExerciseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
