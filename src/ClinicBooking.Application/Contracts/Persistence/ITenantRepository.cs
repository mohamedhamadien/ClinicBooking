using ClinicBooking.Domain.Entities;

namespace ClinicBooking.Application.Contracts.Persistence;

public interface ITenantRepository
{
    Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Tenant?> GetByIdentifierAsync(string identifier, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string identifier, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Tenant>> ListAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default);
}
