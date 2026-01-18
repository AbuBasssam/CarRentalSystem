using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EntitiesConfigurations
{
    public class OtpConfig : IEntityTypeConfiguration<Otp>
    {
        public void Configure(EntityTypeBuilder<Otp> builder)
        {
            builder.ToTable("Otps");
            builder.HasKey(o => o.Id);

            // Configure properties


            builder.Property(o => o.Code)
                .HasColumnType("char")
                .HasMaxLength(60)
                .IsRequired();


            builder
                .HasIndex(o => new { o.UserId, o.Type })
                .HasDatabaseName("IX_Otps_ActiveOtp")
                .IsUnique()
                .HasFilter("[IsUsed] = 0");

            builder.Property(o => o.CreationTime).IsRequired();

            builder.Property(o => o.ExpirationTime).IsRequired();

            builder.Property(c => c.Type).HasConversion<int>().IsRequired();

            // Configure the foreign key relationship
            builder.HasOne(o => o.User)
                .WithMany(u => u.Otps)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Cascade);


            builder.ToTable(t => t.HasCheckConstraint("CK_Otp_Type", "Type > 0 AND Type < 3"));


        }
    }


}
