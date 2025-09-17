using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NFIT.Domain.Entities;

namespace NFIT.Persistence.Configurations;

public class GymCheckInConfiguration : IEntityTypeConfiguration<GymCheckIn>
{
    public void Configure(EntityTypeBuilder<GymCheckIn> builder)
    {
        builder.ToTable("GymCheckIns");

        // ➤ User əlaqəsi (AppUser)
        builder.HasOne(gc => gc.User)
               .WithMany(u => u.GymCheckIns)
               .HasForeignKey(gc => gc.UserId)
               .OnDelete(DeleteBehavior.Restrict);

        // ➤ Gym əlaqəsi
        builder.HasOne(gc => gc.Gym)
               .WithMany(g => g.CheckIns)
               .HasForeignKey(gc => gc.GymId)
               .OnDelete(DeleteBehavior.Restrict);

        // ➤ Notes optional
        builder.Property(gc => gc.Notes)
               .HasMaxLength(500);

        // ➤ Status enumu string kimi saxlanmasın (default int)
        builder.Property(gc => gc.Status)
               .HasConversion<int>();

        // ➤ CheckInTime is required
        builder.Property(gc => gc.CheckInTime)
               .IsRequired();
    }
}
