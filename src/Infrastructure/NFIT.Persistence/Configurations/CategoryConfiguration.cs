using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NFIT.Domain.Entities;

namespace NFIT.Persistence.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");

        builder.Property(e => e.Name).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(500);

        builder
       .HasMany(c => c.Gyms)
       .WithMany() // əgər Gym tərəfində belə navigation yoxdursa WithMany() yaz
       .UsingEntity<GymCategory>(
           j => j
               .HasOne(gc => gc.Gym)
               .WithMany()
               .HasForeignKey(gc => gc.GymId)
               .OnDelete(DeleteBehavior.Restrict),

           j => j
               .HasOne(gc => gc.Category)
               .WithMany()
               .HasForeignKey(gc => gc.CategoryId)
               .OnDelete(DeleteBehavior.Restrict),

           j =>
           {
               j.HasKey(gc => gc.Id); // BaseEntity varsa, bu vacib deyil
               j.ToTable("GymCategories");
           });

    }
}
