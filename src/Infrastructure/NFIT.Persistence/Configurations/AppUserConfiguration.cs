using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NFIT.Domain.Entities;

namespace NFIT.Persistence.Configurations;

public class AppUserConfiguration:IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        // Table name override (optional)
        builder.ToTable("Users");

        // Required fields
        builder.Property(u => u.FullName)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(u => u.Gender)
               .IsRequired();

        builder.Property(u => u.FitnessLevel)
               .IsRequired();

        builder.Property(u => u.Payment)
               .IsRequired();

        builder.Property(u => u.ProfilePictureUrl)
               .HasMaxLength(500);

        builder.Property(u => u.Bio)
               .HasMaxLength(1000);

        builder.Property(u => u.Address)
               .HasMaxLength(250);

        // Relationships
        builder.HasMany(u => u.Orders)
               .WithOne(o => o.User)
               .HasForeignKey(o => o.UserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.Favourites)
               .WithOne(f => f.User)
               .HasForeignKey(f => f.UserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.Reviews)
               .WithOne(r => r.User)
               .HasForeignKey(r => r.UserId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(u => u.GymCheckIns)
               .WithOne(c => c.User)
               .HasForeignKey(c => c.UserId)
               .OnDelete(DeleteBehavior.Restrict);

    }
}
