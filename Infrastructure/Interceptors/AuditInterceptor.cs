using Domain.Entities;
using Domain.Enums;
using Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using System.Text.Json;

namespace Infrastructure.Interceptors;

public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly IRequestContext _requestContext;

    private bool _isAuditing = false;// Prevents recursive calls when saving AuditLogs
    private List<EntityEntry> _pendingAddedEntries = new();

    private IDbContextTransaction? _ownedTransaction = null;// To manage our own transaction if none exists

    private static readonly HashSet<string> _excludedProperties =
        new() { "CreatedAt", "UpdatedAt", "RowVersion" };

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false,
    };

    public AuditInterceptor(IRequestContext requestContext)
    {
        _requestContext = requestContext;
    }

    // =========================================================================
    // Phase 1: Before Saving to Database
    // =========================================================================

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        // 1. Skip if we are currently saving an AuditLog to avoid infinite loops
        if (_isAuditing)
            return await base.SavingChangesAsync(eventData, result, cancellationToken);

        var context = eventData.Context;
        if (context == null)
            return await base.SavingChangesAsync(eventData, result, cancellationToken);

        // 2. Identify changes in Branch entities (Modified, Deleted, or Added)
        var branchEntries = GetTrackedBranches(context);

        // 3. Handle Modifications and Deletions immediately (before original values are lost)
        foreach (var entry in branchEntries.Where(e => e.State != EntityState.Added))
        {
            AuditLog? auditLog = entry.State switch
            {
                EntityState.Modified => HandleModified(entry),
                EntityState.Deleted => HandleDeleted(entry),
                _ => null
            };

            if (auditLog != null)
                context.Set<AuditLog>().Add(auditLog);
        }

        // 4. Queue Added entities to process them after saving 

        _pendingAddedEntries = branchEntries
            .Where(e => e.State == EntityState.Added)
            .ToList();

        // 5. Ensure Atomicity: start a transaction if the caller hasn't started one
        // if CurrentTransaction not equal null that's mean there is outside transaction has been start

        if (_pendingAddedEntries.Any() && context.Database.CurrentTransaction == null)
        {
            _ownedTransaction = await context.Database.BeginTransactionAsync(cancellationToken);

        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    // =========================================================================
    // Phase 2: After Saving to Database
    // =========================================================================

    public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        // 1. no new entries were added, we are done
        if (_isAuditing || !_pendingAddedEntries.Any())
            return await base.SavedChangesAsync(eventData, result, cancellationToken);

        var context = eventData.Context;
        if (context == null)
            return await base.SavedChangesAsync(eventData, result, cancellationToken);

        _isAuditing = true;
        try
        {
            // 2. Now the Branch is saved and has an ID (PK). We can create its AuditLog.
            foreach (var entry in _pendingAddedEntries)
                context.Set<AuditLog>().Add(HandleAdded(entry));

            await context.SaveChangesAsync(cancellationToken);

            // 3. Commit the transaction if we were the ones who started it

            if (_ownedTransaction != null)
                await _ownedTransaction.CommitAsync(cancellationToken);
        }
        catch
        {
            if (_ownedTransaction != null)
                await _ownedTransaction.RollbackAsync(cancellationToken);

            throw;
        }
        finally
        {
            await CleanupAsync();
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }


    public override async Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        // Close own Transtion if it exists
        if (_ownedTransaction != null)
        {
            try { await _ownedTransaction.RollbackAsync(cancellationToken); }
            catch { }
        }

        await CleanupAsync();
        await base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    // =========================================================================
    //  Cleanup
    // =========================================================================

    /// <summary>
    /// Resets the interceptor's internal state and releases associated resources.
    /// This method ensures that flags are reset and temporary entity lists are cleared 
    /// to prevent data leakage or incorrect state in subsequent save operations.
    /// It also handles the asynchronous disposal of the database transaction if one was initiated.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    private async ValueTask CleanupAsync()
    {
        _isAuditing = false;
        _pendingAddedEntries.Clear();

        if (_ownedTransaction != null)
        {
            await _ownedTransaction.DisposeAsync();
            _ownedTransaction = null;
        }
    }

    // =========================================================================
    //  Tracking Dispatcher
    // =========================================================================

    /// <summary>
    /// Identify changes in Branch entities (Modified, Deleted, or Added)
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    private static List<EntityEntry> GetTrackedBranches(DbContext context)
    {
        return context.ChangeTracker
            .Entries()
            .Where(e => e.Entity is Branch &&
                   (e.State == EntityState.Added ||
                    e.State == EntityState.Modified ||
                    e.State == EntityState.Deleted))
            .ToList();
    }

    // =========================================================================
    //  Handlers
    // =========================================================================

    /// <summary>
    /// Creates an audit log entry for a newly added entity.
    /// </summary>
    /// <param name="entry">The entity entry representing the added entity.</param>
    /// <returns>An AuditLog object capturing the creation details of the entity.</returns>
    private AuditLog HandleAdded(EntityEntry entry) =>
        CreateAuditLog(entry, enAuditActionType.Creation, null, ExtractNewValues(entry));

    /// <summary>
    /// Creates an audit log entry for modified entities if any property values have changed.
    /// </summary>
    /// <param name="entry">The entity entry to check for modifications.</param>
    /// <returns>An AuditLog object if modifications are detected; otherwise, null.</returns>
    private AuditLog? HandleModified(EntityEntry entry)
    {
        var (oldValues, newValues) = ExtractModifiedValues(entry);
        return oldValues.Any()
            ? CreateAuditLog(entry, enAuditActionType.Modified, oldValues, newValues)
            : null;
    }

    /// <summary>
    /// Creates an audit log entry for a deleted entity.
    /// </summary>
    /// <param name="entry">The entity entry representing the deleted entity.</param>
    /// <returns>An AuditLog object containing details of the deletion.</returns>
    private AuditLog HandleDeleted(EntityEntry entry) =>
        CreateAuditLog(entry, enAuditActionType.Deleted, ExtractOldValues(entry), null);


    /// <summary>
    /// Creates an AuditLog instance representing changes made to an entity.
    /// </summary>
    /// <param name="entry">The EntityEntry containing metadata and state information about the entity being audited.</param>
    /// <param name="action">The type of audit action performed on the entity.</param>
    /// <param name="oldValues">A dictionary of the entity's original property values before the change.</param>
    /// <param name="newValues">A dictionary of the entity's new property values after the change.</param>
    /// <returns>An AuditLog object containing details of the entity change.</returns>
    private AuditLog CreateAuditLog(EntityEntry entry, enAuditActionType action, Dictionary<string, object?>? oldValues, Dictionary<string, object?>? newValues)
    {
        var userId = _requestContext.UserId;

        if (userId == null)
        {
            // For development, you might log a warning, but for production, this is a security breach.
            throw new InvalidOperationException(
                $"Audit tracking failed: Action '{action}' on entity '{entry.Metadata.ClrType.Name}' " +
                "cannot be performed without a valid User Identity.");
        }
        return new()
        {
            Id = Guid.NewGuid(),
            EntityName = entry.Metadata.ClrType.Name,
            EntityId = GetPrimaryKey(entry),
            Action = action,
            ChangedBy = userId.Value,
            ChangeDate = DateTime.UtcNow,
            OldValues = oldValues?.Any() == true ? JsonSerializer.Serialize(oldValues, _jsonOptions) : null,
            NewValues = newValues?.Any() == true ? JsonSerializer.Serialize(newValues, _jsonOptions) : null
        };
    }


    /// <summary>
    /// Extracts a dictionary of property names and their current values from the given entity entry, excluding
    /// properties that should be ignored.
    /// </summary>
    /// <param name="entry">The entity entry from which to extract property values.</param>
    /// <returns>A dictionary mapping property names to their current values.</returns>
    private static Dictionary<string, object?> ExtractNewValues(EntityEntry entry)
    {
        var values = new Dictionary<string, object?>();
        foreach (var property in entry.Properties)
        {
            if (!ShouldIgnore(property))
                values[property.Metadata.Name] = property.CurrentValue;
        }
        return values;
    }

    /// <summary>
    /// Extracts all original values of an entity. 
    /// This is used to maintain a full snapshot of the data as it existed before removal.
    /// </summary>
    /// <param name="entry">The EF Core entity entry being tracked.</param>
    /// <returns>A dictionary containing property names as keys and their original values as values.</returns>
    private static Dictionary<string, object?> ExtractOldValues(EntityEntry entry)
    {
        var values = new Dictionary<string, object?>();
        foreach (var property in entry.Properties)
        {
            if (!ShouldIgnore(property))
                values[property.Metadata.Name] = property.OriginalValue;
        }
        return values;
    }

    /// <summary>
    /// Identifies and extracts only the properties that have been changed during an update operation.
    /// It ignores unchanged properties to keep the audit log concise and focused on actual modifications.
    /// </summary>
    /// <param name="entry">The EF Core entity entry being tracked.</param>
    /// <returns>
    /// A tuple containing two dictionaries: 
    /// 'OldValues' with data before the change, and 'NewValues' with the updated data.
    /// </returns>
    private static (Dictionary<string, object?> OldValues, Dictionary<string, object?> NewValues) ExtractModifiedValues(EntityEntry entry)
    {
        var oldValues = new Dictionary<string, object?>();
        var newValues = new Dictionary<string, object?>();

        foreach (var property in entry.Properties)
        {
            if (ShouldIgnore(property) || !property.IsModified) continue;
            oldValues[property.Metadata.Name] = property.OriginalValue;
            newValues[property.Metadata.Name] = property.CurrentValue;
        }

        return (oldValues, newValues);
    }

    // =========================================================================
    //  Helpers
    // =========================================================================

    /// <summary>
    /// Determines whether a specific property should be excluded from the audit trail.
    /// Primary keys and technical timestamps (like RowVersion) are usually ignored to keep logs clean.
    /// </summary>
    /// <param name="property">The specific property entry to evaluate.</param>
    /// <returns>True if the property matches the ignore criteria; otherwise, false.</returns>
    private static bool ShouldIgnore(PropertyEntry property) =>
    property.Metadata.IsPrimaryKey() ||
        _excludedProperties.Contains(property.Metadata.Name);

    /// <summary>
    /// Dynamically retrieves the Primary Key value of the entity being audited.
    /// This allows the system to link the audit log to the specific record in the database.
    /// </summary>
    /// <param name="entry">The EF Core entity entry.</param>
    /// <returns>The integer value of the entity's Primary Key.</returns>
    private static int GetPrimaryKey(EntityEntry entry) =>
        (int)entry.Properties.First(p => p.Metadata.IsPrimaryKey()).CurrentValue!;
}