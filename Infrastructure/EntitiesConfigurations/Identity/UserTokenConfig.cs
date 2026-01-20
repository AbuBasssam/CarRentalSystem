using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class UserTokenConfig : IEntityTypeConfiguration<UserToken>
{
    public void Configure(EntityTypeBuilder<UserToken> builder)
    {
        builder.ToTable(name: "UserTokens");

        builder.HasIndex(o => new { o.UserId, o.Type })
               .HasDatabaseName("IX_UserTokens_UsedToken")
               .IsUnique()
               .HasFilter("[IsUsed] = 0");
        builder.ToTable(t => t.HasCheckConstraint("CK_Token_Type", "Type > 0 AND Type < 3"));

    }
}