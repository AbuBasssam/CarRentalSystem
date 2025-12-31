using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EntitiesConfigurations
{
    public class UserConfig : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable(name: "Users");
            builder.Property(u => u.FirstName).HasMaxLength(20).IsRequired();
            builder.Property(u => u.LastName).HasMaxLength(20).IsRequired();

            builder.Property(x => x.PhoneNumber).HasMaxLength(20);
            builder.Property(u => u.ImagePath).HasMaxLength(250).IsRequired(false);
            builder.Ignore(u => u.TwoFactorEnabled);
            builder.Ignore(u => u.PhoneNumber);
            builder.Ignore(u => u.PhoneNumberConfirmed);

        }
    }

}
