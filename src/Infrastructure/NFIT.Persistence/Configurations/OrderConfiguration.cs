using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NFIT.Domain.Entities;

namespace NFIT.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.Property(o => o.TotalPrice)
               .HasPrecision(18, 2); // Ənənəvi decimal dəqiqliyi

        builder.Property(o => o.Note)
               .HasMaxLength(1000);

        builder.Property(o => o.DeliveryAddress)
               .HasMaxLength(500);

        builder.Property(o => o.PaymentMethod)
               .IsRequired();

        builder.Property(o => o.OrderDate)
               .IsRequired();

        builder.Property(o => o.Status)
               .IsRequired();

        // ➤ Order → AppUser
        builder.HasOne(o => o.User)
               .WithMany(u => u.Orders)
               .HasForeignKey(o => o.UserId)
               .OnDelete(DeleteBehavior.Restrict);

        // ➤ Order → OrderSupplements (1-to-many)
        builder.HasMany(o => o.OrderSupplements)
               .WithOne(os => os.Order)
               .HasForeignKey(os => os.OrderId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.SubscriptionPlan)
       .WithMany()                        // əgər SubscriptionPlan.Orders yoxdursa
       .HasForeignKey(o => o.SubscriptionPlanId) // Guid? (nullable)
       .OnDelete(DeleteBehavior.Restrict);
    }
}
