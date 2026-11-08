using ClinicBooking.Application.Contracts.Services;

namespace ClinicBooking.API.Services;

public interface IMutableTenantContext : ITenantContext
{
    void SetTenant(Guid tenantId, string tenantIdentifier);
    void SetHost();
}

public sealed class TenantContext : IMutableTenantContext
{
    public Guid? TenantId { get; private set; }
    public string? TenantIdentifier { get; private set; }
    public bool IsHost { get; private set; }

    public void SetTenant(Guid tenantId, string tenantIdentifier)
    {
        TenantId = tenantId;
        TenantIdentifier = tenantIdentifier;
        IsHost = false;
    }

    public void SetHost()
    {
        TenantId = null;
        TenantIdentifier = null;
        IsHost = true;
    }
}
