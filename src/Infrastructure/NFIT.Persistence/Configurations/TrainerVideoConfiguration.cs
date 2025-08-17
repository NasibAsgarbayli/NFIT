using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NFIT.Domain.Entities;

namespace NFIT.Persistence.Configurations;

public class TrainerVideoConfiguration : IEntityTypeConfiguration<TrainerVideo>
{
    public void Configure(EntityTypeBuilder<TrainerVideo> builder)
    {
        builder.Property(v => v.Title)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(v => v.Description)
            .HasMaxLength(1000);

        builder.Property(v => v.VideoUrl)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(v => v.ThumbnailUrl)
            .HasMaxLength(300);

        builder.Property(v => v.Duration)
            .IsRequired();

        builder.Property(v => v.Type)
            .IsRequired();

        builder.Property(v => v.Category)
            .IsRequired(false);

        builder.Property(v => v.ViewCount)
            .HasDefaultValue(0);

        builder.Property(v => v.LikeCount)
            .HasDefaultValue(0);

        builder.Property(v => v.IsPremium)
            .IsRequired();

        builder.Property(v => v.PublishedAt)
            .IsRequired();

        builder.HasOne(v => v.Trainer)
            .WithMany(t => t.TrainerVideos)
            .HasForeignKey(v => v.TrainerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable("TrainerVideos");
    }
}
