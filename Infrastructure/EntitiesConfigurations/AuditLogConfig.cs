using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EntitiesConfigurations;

public class AuditLogConfig : IEntityTypeConfiguration<AuditLog>
{
    private static readonly int _minAction = (int)Enum.GetValues<enAuditActionType>().Min();
    private static readonly int _maxAction = (int)Enum.GetValues<enAuditActionType>().Max();

    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.Property(a => a.Action)
              .IsRequired()
              .HasConversion<byte>()
              .HasComment($"Action type: {FormatEnumComment()}");

        builder.ToTable
        (
            t => t.HasCheckConstraint(
                name: "CK_AuditLogs_Action_ValidRange",
                sql: $"[Action] BETWEEN {_minAction} AND {_maxAction}"
            )
        );

        builder.HasOne(a => a.User)
             .WithMany()
             .HasForeignKey(a => a.ChangedBy)
             .OnDelete(DeleteBehavior.Restrict);

    }
    private static string FormatEnumComment() =>
       string.Join(", ",
           Enum.GetValues<enAuditActionType>()
               .Select(e => $"{e}={(int)e}"));
}

