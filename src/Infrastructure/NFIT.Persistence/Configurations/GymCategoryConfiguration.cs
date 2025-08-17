using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NFIT.Domain.Entities;

namespace NFIT.Persistence.Configurations;

public class GymCategoryConfiguration : IEntityTypeConfiguration<GymCategory>
{
    public void Configure(EntityTypeBuilder<GymCategory> builder)
    {
        builder.ToTable("GymCategories");

        // ➤ Hər bir kombinasiya unikal olmalıdır (GYMId + CategoryId)
        builder.HasIndex(gc => new { gc.GYMId, gc.CategoryId }).IsUnique();

        // ➤ GYM əlaqəsi
        builder.HasOne(gc => gc.GYM)
               .WithMany(g => g.GymCategories)
               .HasForeignKey(gc => gc.GYMId)
               .OnDelete(DeleteBehavior.Restrict);

        // ➤ Category əlaqəsi
        builder.HasOne(gc => gc.Category)
               .WithMany()
               .HasForeignKey(gc => gc.CategoryId)
               .OnDelete(DeleteBehavior.Restrict);
    }

}
