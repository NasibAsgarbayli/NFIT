using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NFIT.Domain.Entities;

namespace NFIT.Persistence.Configurations;

public class GymQRCodeConfiguration : IEntityTypeConfiguration<GymQRCode>
{
    public void Configure(EntityTypeBuilder<GymQRCode> builder)
    {
        builder.ToTable("GymQRCodes");

        // ➤ One-to-One: Gym <-> GymQRCode
        builder.HasOne(qr => qr.Gym)
               .WithOne(g => g.QRCode)
               .HasForeignKey<GymQRCode>(qr => qr.GymId)
               .OnDelete(DeleteBehavior.Restrict);

        // ➤ QRCodeData - required and unique
        builder.Property(qr => qr.QRCodeData)
               .IsRequired()
               .HasMaxLength(255);

        builder.HasIndex(qr => qr.QRCodeData)
               .IsUnique();

        // ➤ IsActive - default true already set in model
        builder.Property(qr => qr.IsActive)
               .HasDefaultValue(true);
    }
}
