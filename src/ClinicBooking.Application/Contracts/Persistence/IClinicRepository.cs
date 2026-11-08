using ClinicBooking.Domain.Entities;

namespace ClinicBooking.Application.Contracts.Persistence;

public interface IClinicRepository
{
    Task<Clinic?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Clinic>> ListAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task AddAsync(Clinic clinic, CancellationToken cancellationToken = default);
    Task UpdateAsync(Clinic clinic, CancellationToken cancellationToken = default);
}
