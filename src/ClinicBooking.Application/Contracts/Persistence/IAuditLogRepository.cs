using ClinicBooking.Domain.Entities;

namespace ClinicBooking.Application.Contracts.Persistence;

public interface IAuditLogRepository
{
    Task<IReadOnlyList<AuditLog>> ListAsync(
        Guid? tenantId,
        string? entityName = null,
        int limit = 100,
        CancellationToken cancellationToken = default);
}
