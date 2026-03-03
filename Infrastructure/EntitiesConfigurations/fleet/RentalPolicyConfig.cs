using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EntitiesConfigurations;

public class RentalPolicyConfig : IEntityTypeConfiguration<RentalPolicy>
{
    public void Configure(EntityTypeBuilder<RentalPolicy> builder)
    {
        builder.ToTable("RentalPolicies");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .IsUnicode(false)
            .HasMaxLength(100);

        builder.Property(x => x.BufferHours).IsRequired();

        builder.Property(x => x.AllowDifferentDropOff)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.MinCancellationLeadTimeHours).IsRequired();

        builder.Property(x => x.CancellationPenaltyPercent)
            .IsRequired()
            .HasColumnType("decimal(5,2)");

        builder.Property(x => x.NoShowPenaltyDays).IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.CreatedAt).IsRequired();

        // Relations
        builder.HasMany(x => x.Categories)
            .WithOne(x => x.Policy)
            .HasForeignKey(x => x.PolicyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.CarOverrides)
            .WithOne(x => x.PolicyOverride)
            .HasForeignKey(x => x.PolicyOverrideId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
    }
}