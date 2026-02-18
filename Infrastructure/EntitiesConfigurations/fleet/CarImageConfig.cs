using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EntitiesConfigurations;

public class CarImageConfig : IEntityTypeConfiguration<CarImage>
{
    public void Configure(EntityTypeBuilder<CarImage> builder)
    {
        builder.ToTable("CarImages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ImageUrl)
            .IsRequired()
            .HasMaxLength(350);

        builder.Property(x => x.IsPrimary)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasIndex(x => new { x.CarId, x.IsPrimary });
    }
}
