namespace ClinicBooking.Application.Contracts.Services;

public interface ITenantContext
{
    Guid? TenantId { get; }
    string? TenantIdentifier { get; }
    bool IsHost { get; }
}
