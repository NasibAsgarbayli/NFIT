using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NFIT.Domain.Entities;

namespace NFIT.Persistence.Configurations;

public class GymConfiguration: IEntityTypeConfiguration<Gym>
{
    public void Configure(EntityTypeBuilder<Gym> builder)
    {
        builder.ToTable("Gyms");

        builder.Property(g => g.Name)
               .HasMaxLength(100);

        builder.Property(g => g.Description)
               .HasMaxLength(1000);

        builder.Property(g => g.Address)
               .HasMaxLength(300);

        builder.Property(g => g.Phone)
               .HasMaxLength(20);

        builder.Property(g => g.Email)
               .HasMaxLength(100);

        builder.Property(g => g.Website)
               .HasMaxLength(200);

        builder.Property(g => g.InstagramLink)
               .HasMaxLength(200);

        builder.Property(g => g.Latitude)
               .HasPrecision(9, 6); // GPS üçün uyğun format

        builder.Property(g => g.Longitude)
               .HasPrecision(9, 6);

        // ➤ District əlaqəsi (Required)
        builder.HasOne(g => g.District)
               .WithMany(d => d.Gyms)
               .HasForeignKey(g => g.DistrictId)
               .OnDelete(DeleteBehavior.Restrict);

        // ➤ QR Code əlaqəsi (1:1)
        builder.HasOne(g => g.QRCode)
               .WithOne(q => q.Gym)
               .HasForeignKey<GymQRCode>(q => q.GymId)
               .OnDelete(DeleteBehavior.Restrict);

        // ➤ GymCategory (M:N → Gym ↔ GymCategory ↔ Category)
        builder.HasMany(g => g.GymCategories)
               .WithOne(gc => gc.Gym)
               .HasForeignKey(gc => gc.GymId)
               .OnDelete(DeleteBehavior.Restrict);

        // ➤ Review əlaqəsi
        builder.HasMany(g => g.Reviews)
               .WithOne(r => r.Gym)
               .HasForeignKey(r => r.GymID)
               .OnDelete(DeleteBehavior.Restrict);

        // ➤ SubscriptionPlan əlaqəsi (Gym → AvailableSubscriptions)
        builder.HasMany(g => g.AvailableSubscriptions)
               .WithMany(s => s.Gyms);

        // ➤ Image əlaqəsi
        builder.HasMany(g => g.Images)
               .WithOne(i => i.Gym)
               .HasForeignKey(i => i.GymId)
               .OnDelete(DeleteBehavior.Restrict);

        // ➤ Favourite əlaqəsi
        builder.HasMany(g => g.Favourites)
               .WithOne(f => f.Gym)
               .HasForeignKey(f => f.GymId)
               .OnDelete(DeleteBehavior.Restrict);

        // ➤ GymCheckIn əlaqəsi
        builder.HasMany(g => g.CheckIns)
               .WithOne(c => c.Gym)
               .HasForeignKey(c => c.GymId)
               .OnDelete(DeleteBehavior.Restrict);

    }
}
