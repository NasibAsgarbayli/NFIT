using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NFIT.Domain.Entities;

namespace NFIT.Persistence.Configurations;

public class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        builder.ToTable("SubscriptionPlans");

        // Description - məcburidir və maksimum uzunluğu müəyyənləşdir
        builder.Property(sp => sp.Description)
               .IsRequired()
               .HasMaxLength(1000);

        // Price - mütləq tələb olunur
        builder.Property(sp => sp.Price)
               .HasColumnType("decimal(10,2)")
               .IsRequired();

        // Type və BillingCycle enumlardır
        builder.Property(sp => sp.Type)
               .IsRequired();

        builder.Property(sp => sp.BillingCycle)
               .IsRequired();

        // GYMS ilə əlaqə (bir planı bir neçə gym istifadə edə bilər)
        builder.HasMany(sp => sp.GYMS)
               .WithMany(g => g.AvailableSubscriptions)
               .UsingEntity<Dictionary<string, object>>(
                    "GymSubscriptionPlan",
                    j => j
                        .HasOne<NFIT.Domain.Entities.Gym>()
                        .WithMany()
                        .HasForeignKey("GymId")
                        .OnDelete(DeleteBehavior.Restrict),
                    j => j
                        .HasOne<SubscriptionPlan>()
                        .WithMany()
                        .HasForeignKey("SubscriptionPlanId")
                        .OnDelete(DeleteBehavior.Restrict),
                    j =>
                    {
                        j.HasKey("GymId", "SubscriptionPlanId");
                        j.ToTable("GymSubscriptionPlans");
                    });
    }
}
