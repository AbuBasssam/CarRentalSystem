using Domain.Entities;
using Domain.Enums;
using Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Text.Json;

namespace Infrastructure.Interceptors;
public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly IRequestContext _requestContext;

    public AuditInterceptor(IRequestContext requestContext)
    {
        _requestContext = requestContext;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;

        if (context == null)
            return await base.SavingChangesAsync(eventData, result, cancellationToken);

        var auditEntries = context.ChangeTracker
            .Entries()
            .Where(e => e.Entity is Branch &&
                        (e.State == EntityState.Modified ||
                         e.State == EntityState.Deleted))
            .ToList();

        foreach (var entry in auditEntries)
        {
            var auditLog = _CreateAuditEntry(entry);
            context.Set<AuditLog>().Add(auditLog);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private AuditLog _CreateAuditEntry(EntityEntry entry)
    {
        var oldValues = new Dictionary<string, object?>();
        var newValues = new Dictionary<string, object?>();

        var excludedProperties = new HashSet<string>
        {
            "CreatedAt",
            "UpdatedAt",
            "RowVersion"
        };

        foreach (var property in entry.Properties)
        {
            var propertyName = property.Metadata.Name;

            if (excludedProperties.Contains(propertyName))
                continue;

            // لا نريد Navigation Properties
            if (property.Metadata.IsPrimaryKey())
                continue;

            switch (entry.State)
            {
                case EntityState.Added:
                    newValues[propertyName] = property.CurrentValue;
                    break;

                case EntityState.Deleted:
                    oldValues[propertyName] = property.OriginalValue;
                    break;

                case EntityState.Modified:
                    if (property.IsModified)
                    {
                        oldValues[propertyName] = property.OriginalValue;
                        newValues[propertyName] = property.CurrentValue;
                    }
                    break;
            }
        }

        return new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityName = entry.Metadata.ClrType.Name,
            EntityId = (int)entry.Properties
                .First(p => p.Metadata.IsPrimaryKey())
                .CurrentValue!,

            Action = entry.State switch
            {
                EntityState.Added => enAuditActionType.Creation,
                EntityState.Modified => enAuditActionType.Modified,
                EntityState.Deleted => enAuditActionType.Deleted,
                _ => throw new InvalidOperationException("Unsupported audit state")
            },

            ChangedBy = _requestContext.UserId ?? 0,
            ChangeDate = DateTime.UtcNow,

            OldValues = oldValues.Any()
                ? JsonSerializer.Serialize(oldValues)
                : null,

            NewValues = newValues.Any()
                ? JsonSerializer.Serialize(newValues)
                : null
        };
    }
}

