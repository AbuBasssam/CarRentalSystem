using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EntitiesConfigurations;

public class CarBranchHistoryConfig : IEntityTypeConfiguration<CarBranchHistory>
{
    public void Configure(EntityTypeBuilder<CarBranchHistory> builder)
    {
        builder.ToTable("CarBranchHistories");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.MovedAt)
            .IsRequired();

        builder.Property(x => x.Reason)
            .HasMaxLength(250);

        // Index To search for the history of a specific car
        builder.HasIndex(x => new { x.CarId, x.MovedAt });
    }
}