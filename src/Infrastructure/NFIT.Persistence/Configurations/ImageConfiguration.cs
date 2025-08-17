using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NFIT.Domain.Entities;

namespace NFIT.Persistence.Configurations;

public class ImageConfiguration : IEntityTypeConfiguration<Image>
{
    public void Configure(EntityTypeBuilder<Image> builder)
    {
        builder.ToTable("Images");

        // ➤ ImageUrl required
        builder.Property(i => i.ImageUrl)
               .IsRequired()
               .HasMaxLength(500);

        // ➤ Optional: Image -> Gym
        builder.HasOne(i => i.Gym)
               .WithMany(g => g.Images)
               .HasForeignKey(i => i.GymId)
               .OnDelete(DeleteBehavior.Restrict);

        // ➤ Optional: Image -> Supplement
        builder.HasOne(i => i.Supplement)
               .WithMany(s => s.Images)
               .HasForeignKey(i => i.SupplementId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
