using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NFIT.Domain.Entities;

namespace NFIT.Persistence.Configurations;

public class GymCategoryConfiguration : IEntityTypeConfiguration<GymCategory>
{
    public void Configure(EntityTypeBuilder<GymCategory> builder)
    {
        builder.ToTable("GymCategories");

        // ➤ Hər bir kombinasiya unikal olmalıdır (GymId + CategoryId)
        builder.HasIndex(gc => new { gc.GymId, gc.CategoryId }).IsUnique();

        // ➤ Gym əlaqəsi
        builder.HasOne(gc => gc.Gym)
               .WithMany(g => g.GymCategories)
               .HasForeignKey(gc => gc.GymId)
               .OnDelete(DeleteBehavior.Restrict);

        // ➤ Category əlaqəsi
        builder.HasOne(gc => gc.Category)
               .WithMany()
               .HasForeignKey(gc => gc.CategoryId)
               .OnDelete(DeleteBehavior.Restrict);
    }

}
