using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NFIT.Domain.Entities;

namespace NFIT.Persistence.Configurations;

public class DistrictConfiguration: IEntityTypeConfiguration<District>
{
    public void Configure(EntityTypeBuilder<District> builder)
    {
        builder.ToTable("Districts");

        builder.Property(d => d.Name)
               .HasMaxLength(100)
               .IsRequired();

        builder.Property(d => d.IsActive)
               .HasDefaultValue(true);

        builder.Ignore(e => e.City);

        // City computed property-dir, bu EF-də map olunmamalıdır
        builder.Ignore(d => d.City);

        // Relation: One District → Many Gyms
        builder.HasMany(d => d.Gyms)
               .WithOne(g => g.District)
               .HasForeignKey(g => g.DistrictId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
