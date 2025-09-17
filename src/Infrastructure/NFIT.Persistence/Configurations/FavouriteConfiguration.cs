using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NFIT.Domain.Entities;

namespace NFIT.Persistence.Configurations;

public class FavouriteConfiguration
{
    public void Configure(EntityTypeBuilder<Favourite> builder)
    {
        builder.ToTable("Favourites");

        builder.Property(f => f.AddedAt)
               .IsRequired(false);

        // User FK
        builder.HasOne(f => f.User)
               .WithMany(u => u.Favourites)
               .HasForeignKey(f => f.UserId)
               .OnDelete(DeleteBehavior.Restrict);

        // Gym FK
        builder.HasOne(f => f.Gym)
               .WithMany(g => g.Favourites)
               .HasForeignKey(f => f.GymId)
               .OnDelete(DeleteBehavior.Restrict);

        // Trainer FK
        builder.HasOne(f => f.Trainer)
               .WithMany(t => t.Favourites)
               .HasForeignKey(f => f.TrainerId)
               .OnDelete(DeleteBehavior.Restrict);

        // Supplement FK
        builder.HasOne(f => f.Supplement)
               .WithMany(s => s.Favourites)
               .HasForeignKey(f => f.SupplementId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
