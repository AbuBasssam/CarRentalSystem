using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EntitiesConfigurations;

public class BranchConfig : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("Branches");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.NameEN)
            .IsUnicode(false)
            .IsRequired()
            .HasMaxLength(75);

        builder.Property(x => x.NameAR)
            .IsUnicode(true)
            .IsRequired()
            .HasMaxLength(75);

        builder.Property(x => x.CityEN)
            .IsUnicode(false)
            .IsRequired()
            .HasMaxLength(75);

        builder.Property(x => x.CityAR)
            .IsUnicode(true)
            .IsRequired()
            .HasMaxLength(75);

        builder.Property(x => x.Latitude)
            .IsRequired();

        builder.Property(x => x.Longitude)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

    }

}