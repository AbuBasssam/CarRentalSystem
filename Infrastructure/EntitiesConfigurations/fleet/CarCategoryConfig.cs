using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EntitiesConfigurations;

public class CarCategoryConfig : IEntityTypeConfiguration<CarCategory>
{
    public void Configure(EntityTypeBuilder<CarCategory> builder)
    {
        builder.ToTable("CarCategories");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.NameEN)
            .IsUnicode(false)
            .IsRequired()
            .HasMaxLength(75);

        builder.Property(x => x.NameAR)
            .IsUnicode(true)
            .IsRequired()
            .HasMaxLength(75);

        builder.Property(x => x.IsModelSpecific)
            .IsRequired()
            .HasDefaultValue(false);



        builder.Property(x => x.BaseDailyRate)
            .IsRequired()
            .HasColumnType("decimal(8,2)");

        builder.Property(x => x.BaseWeeklyRate)
            .IsRequired()
            .HasColumnType("decimal(8,2)");

        builder.Property(x => x.BaseMonthlyRate)
            .IsRequired()
            .HasColumnType("decimal(8,2)");

        builder.Property(x => x.AllowDifferentDropOff)
            .IsRequired()
            .HasDefaultValue(true);


        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.Description).HasMaxLength(350);

        builder.Property(x => x.CreatedAt).IsRequired();

        // Relations
        builder.HasMany(x => x.Cars)
            .WithOne(x => x.Category)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
