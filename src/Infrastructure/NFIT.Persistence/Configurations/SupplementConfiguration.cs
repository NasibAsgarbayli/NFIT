using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NFIT.Domain.Entities;

namespace NFIT.Persistence.Configurations;

public class SupplementConfiguration : IEntityTypeConfiguration<Supplement>
{
    public void Configure(EntityTypeBuilder<Supplement> builder)
    {
        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(s => s.Description)
            .HasMaxLength(1000);

        builder.Property(s => s.Price)
            .HasColumnType("decimal(10,2)");

        builder.Property(s => s.Weight)
            .HasColumnType("decimal(10,2)");

        builder.HasMany(s => s.Orders)
            .WithOne(os => os.Supplement)
            .HasForeignKey(os => os.SupplementId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.Favourites)
            .WithOne(f => f.Supplement)
            .HasForeignKey(f => f.SupplementId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.Reviews)
            .WithOne(r => r.Supplement)
            .HasForeignKey(r => r.SupplementId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.Images)
            .WithOne(i => i.Supplement)
            .HasForeignKey(i => i.SupplementId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable("Supplements");
    }
}
