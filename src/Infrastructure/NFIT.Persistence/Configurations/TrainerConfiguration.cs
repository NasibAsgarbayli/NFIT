using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NFIT.Domain.Entities;

namespace NFIT.Persistence.Configurations;

public class TrainerConfiguration : IEntityTypeConfiguration<Trainer>
{
    public void Configure(EntityTypeBuilder<Trainer> builder)
    {
        builder.ToTable("Trainers");

        builder.Property(t => t.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.Bio)
            .HasMaxLength(1000);

        builder.Property(t => t.Specializations)
            .HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries));

        builder.Property(t => t.Certifications)
            .HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries));


        builder.Property(t => t.InstagramUrl)
            .HasMaxLength(300);

        builder.Property(t => t.YoutubeUrl)
            .HasMaxLength(300);

        builder.Property(t => t.Rating)
            .HasColumnType("decimal(3,2)");

        builder.HasMany(t => t.TrainerWorkouts)
            .WithOne(w => w.Trainer)
            .HasForeignKey(w => w.TrainerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.TrainerVideos)
            .WithOne(v => v.Trainer)
            .HasForeignKey(v => v.TrainerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.Favourites)
            .WithOne(f => f.Trainer)
            .HasForeignKey(f => f.TrainerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.Reviews)
            .WithOne(r => r.Trainer)
            .HasForeignKey(r => r.TrainerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.Images)
                .WithOne(i => i.Trainer)
                .HasForeignKey(i => i.TrainerId)
                .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.User)
                .WithMany(u => u.Trainers)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

    }
}
