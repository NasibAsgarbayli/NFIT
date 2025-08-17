using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NFIT.Domain.Entities;

namespace NFIT.Persistence.Configurations;

public class MembershipConfiguration : IEntityTypeConfiguration<Membership>
{
    public void Configure(EntityTypeBuilder<Membership> builder)
    {
        builder.ToTable("Memberships");

        builder.Property(m => m.StartDate)
               .IsRequired();

        builder.Property(m => m.EndDate)
               .IsRequired();

        builder.Property(m => m.IsActive)
               .IsRequired();

        // ➤ Membership -> AppUser
        builder.HasOne(m => m.User)
               .WithMany()
               .HasForeignKey(m => m.UserId)
               .OnDelete(DeleteBehavior.Restrict);

        // ➤ Membership -> Gym
        builder.HasOne(m => m.GYM)
               .WithMany()
               .HasForeignKey(m => m.GymId)
               .OnDelete(DeleteBehavior.Restrict);

        // ➤ Membership -> SubscriptionPlan
        builder.HasOne(m => m.SubscriptionPlan)
               .WithMany()
               .HasForeignKey(m => m.SubscriptionPlanId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
