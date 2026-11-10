using ClinicBooking.Domain.Common;

namespace ClinicBooking.Domain.Entities;

public sealed class AuditLog : Entity<Guid>, IMayHaveTenant
{
    public Guid? TenantId { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string ChangedBy { get; set; } = string.Empty;
    public DateTime ChangedAtUtc { get; set; }
    public string? ChangesJson { get; set; }

    public AuditLog() : base(Guid.NewGuid()) { }

    public AuditLog(
        Guid id,
        Guid? tenantId,
        string entityName,
        string entityId,
        string action,
        string changedBy,
        DateTime changedAtUtc,
        string? changesJson = null) : base(id)
    {
        TenantId = tenantId;
        EntityName = entityName;
        EntityId = entityId;
        Action = action;
        ChangedBy = changedBy;
        ChangedAtUtc = changedAtUtc;
        ChangesJson = changesJson;
    }
}
