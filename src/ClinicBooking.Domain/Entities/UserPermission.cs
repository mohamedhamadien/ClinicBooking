using ClinicBooking.Domain.Common;

namespace ClinicBooking.Domain.Entities;

public sealed class UserPermission : Entity<Guid>, IMayHaveTenant
{
    public Guid UserId { get; private set; }
    public string Permission { get; private set; } = default!;
    public Guid? TenantId { get; private set; }
    public DateTime GrantedAtUtc { get; private set; }

    private UserPermission() { }

    public UserPermission(Guid id, Guid userId, string permission, Guid? tenantId, DateTime grantedAtUtc) : base(id)
    {
        UserId = userId;
        Permission = permission ?? throw new ArgumentNullException(nameof(permission));
        TenantId = tenantId;
        GrantedAtUtc = grantedAtUtc;
    }
}
