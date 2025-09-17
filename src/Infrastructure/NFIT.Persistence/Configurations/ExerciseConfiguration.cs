using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NFIT.Domain.Entities;
using NFIT.Domain.Enums;

namespace NFIT.Persistence.Configurations;

public class ExerciseConfiguration
{
    public void Configure(EntityTypeBuilder<Exercise> builder)
    {
        builder.ToTable("Exercises");

        builder.Property(e => e.Name)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(e => e.Description)
               .HasMaxLength(500);

        builder.Property(e => e.PrimaryMuscleGroup)
               .IsRequired()
               .HasConversion<string>(); // Enum string kimi saxlanacaq

        builder.Property(e => e.Equipment)
               .IsRequired()
               .HasConversion<string>();

        builder.Property(e => e.Difficulty)
               .IsRequired()
               .HasConversion<string>();

        builder.Property(e => e.VideoUrl)
               .HasMaxLength(500);


        // SecondaryMuscleGroups array olduğu üçün EF onu map edə bilməz — ignore edirik
        builder.Ignore(e => e.SecondaryMuscleGroups);

        // Relation: One Exercise → Many WorkoutExercise
        builder.HasMany(e => e.WorkoutExercises)
               .WithOne(we => we.Exercise)
               .HasForeignKey(we => we.ExerciseId)
               .OnDelete(DeleteBehavior.Restrict);
    }

}
