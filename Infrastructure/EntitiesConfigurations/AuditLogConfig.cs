using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EntitiesConfigurations;

public class AuditLogConfig : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasOne(a => a.User)
             .WithMany()
             .HasForeignKey(a => a.ChangedBy)
             .OnDelete(DeleteBehavior.SetNull);

    }
}

