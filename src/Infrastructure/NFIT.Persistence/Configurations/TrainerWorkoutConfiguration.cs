using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NFIT.Domain.Entities;

namespace NFIT.Persistence.Configurations;

public class TrainerWorkoutConfiguration : IEntityTypeConfiguration<TrainerWorkout>
{
    public void Configure(EntityTypeBuilder<TrainerWorkout> builder)
    {
        builder.Property(w => w.Title)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(w => w.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(w => w.Category)
            .IsRequired();

        builder.Property(w => w.Difficulty)
            .IsRequired();

        builder.Property(w => w.EstimatedDuration)
            .IsRequired();

        builder.Property(w => w.TargetMuscles)
            .IsRequired();

        builder.Property(w => w.RequiredEquipment)
            .IsRequired();

        builder.Property(w => w.ThumbnailUrl)
            .HasMaxLength(300);

        builder.Property(w => w.PreviewVideoUrl)
            .HasMaxLength(300);

        builder.Property(w => w.IsPremium)
            .IsRequired();

        builder.Property(w => w.ViewCount)
            .HasDefaultValue(0);

        builder.Property(w => w.LikeCount)
            .HasDefaultValue(0);

        builder.Property(w => w.PublishedAt)
            .IsRequired();

        builder.HasOne(w => w.Trainer)
            .WithMany(t => t.TrainerWorkouts)
            .HasForeignKey(w => w.TrainerId)
            .OnDelete(DeleteBehavior.Restrict);


        builder.HasMany(w => w.WorkoutExercises)
               .WithOne(we => we.TrainerWorkout)
               .HasForeignKey(we => we.TrainerWorkoutId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable("TrainerWorkouts");
    }
}
