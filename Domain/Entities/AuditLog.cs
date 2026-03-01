using Domain.Enums;

namespace Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; }

    public string EntityName { get; set; } = null!;
    public int EntityId { get; set; }

    public enAuditActionType Action { get; set; } // {Creation | Modified | Deleted}

    public int ChangedBy { get; set; }
    public DateTime ChangeDate { get; set; }

    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public virtual User User { get; set; } = null!;
}
