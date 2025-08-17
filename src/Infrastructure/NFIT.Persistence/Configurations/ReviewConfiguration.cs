using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NFIT.Domain.Entities;

namespace NFIT.Persistence.Configurations;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("Reviews");

        // Content sahəsi üçün maksimum uzunluq
        builder.Property(r => r.Content)
               .HasMaxLength(1000);

        builder.Property(r => r.Rating)
               .IsRequired();

        builder.Property(r => r.CreatedAt)
               .IsRequired();

        builder.Property(r => r.IsApproved)
               .IsRequired();

        // Gym əlaqəsi (nullable FK)
        builder.HasOne(r => r.Gym)
               .WithMany(g => g.Reviews)
               .HasForeignKey(r => r.GymID)
               .OnDelete(DeleteBehavior.Restrict);

        // Trainer əlaqəsi (nullable FK)
        builder.HasOne(r => r.Trainer)
               .WithMany(t => t.Reviews)
               .HasForeignKey(r => r.TrainerId)
               .OnDelete(DeleteBehavior.Restrict);

        // Supplement əlaqəsi (nullable FK)
        builder.HasOne(r => r.Supplement)
               .WithMany(s => s.Reviews)
               .HasForeignKey(r => r.SupplementId)
               .OnDelete(DeleteBehavior.Restrict);

        // AppUser əlaqəsi (nullable FK)
        builder.HasOne(r => r.User)
               .WithMany(u => u.Reviews)
               .HasForeignKey(r => r.UserId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
