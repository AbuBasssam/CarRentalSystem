using Domain.Entities;
using Domain.Enums;
using Domain.HelperClasses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EntitiesConfigurations;

public class CarConfig : IEntityTypeConfiguration<Car>
{
    public void Configure(EntityTypeBuilder<Car> builder)
    {
        builder.ToTable("Cars");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.PlateNumberEN)
            .HasMaxLength(10)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(x => x.PlateNumberAR)
            .HasMaxLength(15)
            .IsUnicode(true)
            .IsRequired();

        builder.Property(x => x.VIN)
            .HasMaxLength(17)
            .IsFixedLength()
            .IsUnicode(false)
            .IsRequired();

        builder.Property(x => x.Brand)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Model)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Year)
            .IsRequired();

        builder.Property(x => x.FuelType)
            .IsRequired();

        builder.Property(x => x.TransmissionType)
            .IsRequired()
            .HasConversion<byte>();



        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.FleetConditionStatus)
            .IsRequired()
            .HasConversion<byte>()
            .HasDefaultValue(enFleetConditionStatus.Ready);

        builder.Property(x => x.KmMileage)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.NumberOfSeats).IsRequired();

        builder.Property(x => x.NumberOfBags).IsRequired();

        builder.Property(x => x.EngineCapacity).IsRequired();

        // Unique Constraints
        builder.HasIndex(x => x.VIN).IsUnique();
        builder.HasIndex(x => x.PlateNumberEN).IsUnique();
        builder.HasIndex(x => x.PlateNumberAR).IsUnique();


        // Check Constraints

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Cars_PlateNumberAR",
            "PlateNumberAR LIKE N'[ء-ي] [ء-ي] [ء-ي] [0-9][0-9][0-9][0-9]'")
        );
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Cars_PlateNumberEN",
            "PlateNumberEN LIKE '[A-Z][A-Z][A-Z] [0-9][0-9][0-9][0-9]'")
        );
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Cars_VIN",
            "LEN(VIN) = 17 AND VIN NOT LIKE N'%[^A-HJ-NPR-Z0-9]%'")
        );

        builder.ToTable(t => t.HasCheckConstraint("CK_Cars_Mileage", "KmMileage >= 0"));

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Cars_FuelType",
            $"FuelType>0 AND FuelType<={FuelType.MaxId} ")
        );

        builder.ToTable(t => t.HasCheckConstraint(
           "CK_Cars_TransmissionType",
           $"TransmissionType>0 AND TransmissionType<=2 ")
        );

        builder.ToTable(t => t.HasCheckConstraint(
           "CK_Cars_FleetConditionStatus",
           $"FleetConditionStatus>0 AND FleetConditionStatus<=3 ")
        );

        // Composite Indexes for searching and filtering
        builder.HasIndex(x => new { x.CurrentBranchId, x.CategoryId });
        builder.HasIndex(x => new { x.IsActive, x.FleetConditionStatus });
        builder.HasIndex(x => x.Brand);
        builder.HasIndex(x => x.FuelType);
        builder.HasIndex(x => x.TransmissionType);

        // Relations
        builder.HasMany(x => x.Images)
            .WithOne(x => x.Car)
            .HasForeignKey(x => x.CarId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.BranchHistories)
            .WithOne(x => x.Car)
            .HasForeignKey(x => x.CarId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.PolicyOverride)
            .WithMany(x => x.CarOverrides)
            .HasForeignKey(x => x.PolicyOverrideId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
    }
}