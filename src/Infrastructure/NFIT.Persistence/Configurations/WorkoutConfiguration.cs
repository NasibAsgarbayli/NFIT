using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NFIT.Domain.Entities;

namespace NFIT.Persistence.Configurations;

public class WorkoutConfiguration : IEntityTypeConfiguration<Workout>
{
    public void Configure(EntityTypeBuilder<Workout> builder)
    {
        builder.ToTable("Workouts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(x => x.EstimatedDuration)
            .IsRequired();

        builder.Property(x => x.Difficulty)
            .IsRequired();

        builder.Property(x => x.Category)
            .IsRequired();

        builder.Property(x => x.TargetMuscles)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.RequiredEquipment)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.VideoUrl)
            .HasMaxLength(300);

        builder.Property(x => x.IsPublic)
            .HasDefaultValue(true);

        // Relation: Workout -> WorkoutExercises (One-to-Many)
        builder.HasMany(x => x.WorkoutExercises)
            .WithOne(x => x.Workout)
            .HasForeignKey(x => x.WorkoutId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
