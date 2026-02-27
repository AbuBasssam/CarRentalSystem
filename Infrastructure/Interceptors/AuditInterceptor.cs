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

    private bool _isAuditing = false;
    private List<EntityEntry> _pendingAddedEntries = new();

    private IDbContextTransaction? _ownedTransaction = null;

    private static readonly HashSet<string> _excludedProperties =
        new() { "CreatedAt", "UpdatedAt", "RowVersion" };

    public AuditInterceptor(IRequestContext requestContext)
    {
        _requestContext = requestContext;
    }

    // =========================================================================
    //  قبل الحفظ
    // =========================================================================

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (_isAuditing)
            return await base.SavingChangesAsync(eventData, result, cancellationToken);

        var context = eventData.Context;
        if (context == null)
            return await base.SavingChangesAsync(eventData, result, cancellationToken);

        var branchEntries = GetTrackedBranches(context);

        // ── Modified / Deleted: نعالجهما قبل الحفظ لأننا نحتاج OriginalValue ──
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

        // ── Added: نحجز الـ entries ونتحقق من حالة الـ Transaction ────────────
        _pendingAddedEntries = branchEntries
            .Where(e => e.State == EntityState.Added)
            .ToList();

        if (_pendingAddedEntries.Any())
        {
            // لو لا توجد Transaction خارجية → نفتح واحدة نحن لضمان الـ Atomicity
            // لو توجد → نستخدمها كما هي بدون أي تدخل
            if (context.Database.CurrentTransaction == null)
            {
                _ownedTransaction = await context.Database
                    .BeginTransactionAsync(cancellationToken);
            }
            // لو CurrentTransaction != null: Transaction خارجية موجودة
            // كلا الـ SaveChanges سيعملان تحتها تلقائياً → لا نحتاج أي شيء
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    // =========================================================================
    //  بعد الحفظ
    // =========================================================================

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (_isAuditing || !_pendingAddedEntries.Any())
            return await base.SavedChangesAsync(eventData, result, cancellationToken);

        var context = eventData.Context;
        if (context == null)
            return await base.SavedChangesAsync(eventData, result, cancellationToken);

        _isAuditing = true;
        try
        {
            // الفرع محفوظ الآن → PK متاح → ننشئ AuditLog
            foreach (var entry in _pendingAddedEntries)
                context.Set<AuditLog>().Add(HandleAdded(entry));

            // نحفظ AuditLog — _isAuditing = true يمنع الحلقة التكرارية
            await context.SaveChangesAsync(cancellationToken);

            // نُغلق الـ Transaction فقط لو نحن مَن فتحها
            // لو كانت خارجية → صاحبها (Service Layer) هو مَن يُغلقها
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

    // =========================================================================
    //  فشل الحفظ
    // =========================================================================

    public override async Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        // نتراجع فقط لو نحن مَن فتح الـ Transaction
        if (_ownedTransaction != null)
        {
            try { await _ownedTransaction.RollbackAsync(cancellationToken); }
            catch { /* تجاهل أخطاء الـ Rollback الثانوية */ }
        }

        await CleanupAsync();
        await base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    // =========================================================================
    //  Cleanup
    // =========================================================================

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

    private AuditLog HandleAdded(EntityEntry entry) =>
        CreateAuditLog(entry, enAuditActionType.Creation, null, ExtractNewValues(entry));

    private AuditLog? HandleModified(EntityEntry entry)
    {
        var (oldValues, newValues) = ExtractModifiedValues(entry);
        return oldValues.Any()
            ? CreateAuditLog(entry, enAuditActionType.Modified, oldValues, newValues)
            : null;
    }

    private AuditLog HandleDeleted(EntityEntry entry) =>
        CreateAuditLog(entry, enAuditActionType.Deleted, ExtractOldValues(entry), null);

    // =========================================================================
    //  Core Audit Builder
    // =========================================================================

    private AuditLog CreateAuditLog(
        EntityEntry entry,
        enAuditActionType action,
        Dictionary<string, object?>? oldValues,
        Dictionary<string, object?>? newValues) =>
        new()
        {
            Id = Guid.NewGuid(),
            EntityName = entry.Metadata.ClrType.Name,
            EntityId = GetPrimaryKey(entry),
            Action = action,
            ChangedBy = _requestContext.UserId,
            ChangeDate = DateTime.UtcNow,
            OldValues = oldValues?.Any() == true ? JsonSerializer.Serialize(oldValues) : null,
            NewValues = newValues?.Any() == true ? JsonSerializer.Serialize(newValues) : null
        };

    // =========================================================================
    //  Value Extractors
    // =========================================================================

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

    private static (Dictionary<string, object?> OldValues, Dictionary<string, object?> NewValues)
        ExtractModifiedValues(EntityEntry entry)
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

    private static bool ShouldIgnore(PropertyEntry property) =>
        property.Metadata.IsPrimaryKey() ||
        _excludedProperties.Contains(property.Metadata.Name);

    private static int GetPrimaryKey(EntityEntry entry) =>
        (int)entry.Properties.First(p => p.Metadata.IsPrimaryKey()).CurrentValue!;
}