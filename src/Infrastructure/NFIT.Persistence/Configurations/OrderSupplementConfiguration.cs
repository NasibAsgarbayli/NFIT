using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NFIT.Domain.Entities;

namespace NFIT.Persistence.Configurations;

public class OrderSupplementConfiguration : IEntityTypeConfiguration<OrderSupplement>
{
    public void Configure(EntityTypeBuilder<OrderSupplement> builder)
    {
        builder.ToTable("OrderSupplements");

        builder.Property(x => x.SupplementPrice)
               .HasPrecision(18, 2);

        // ➤ OrderSupplement → Order
        builder.HasOne(os => os.Order)
               .WithMany(o => o.OrderSupplements)
               .HasForeignKey(os => os.OrderId)
               .OnDelete(DeleteBehavior.Restrict);

        // ➤ OrderSupplement → User
        builder.HasOne(os => os.User)
               .WithMany()
               .HasForeignKey(os => os.UserId)
               .OnDelete(DeleteBehavior.Restrict);

        // ➤ OrderSupplement → Supplement
        builder.HasOne(os => os.Supplement)
               .WithMany(s => s.Orders)
               .HasForeignKey(os => os.SupplementId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
