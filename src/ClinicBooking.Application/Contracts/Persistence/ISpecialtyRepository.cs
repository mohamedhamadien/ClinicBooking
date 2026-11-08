using ClinicBooking.Domain.Entities;

namespace ClinicBooking.Application.Contracts.Persistence;

public interface ISpecialtyRepository
{
    Task<Specialty?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Specialty>> ListAsync(Guid? tenantId = null, CancellationToken cancellationToken = default);
    Task AddAsync(Specialty specialty, CancellationToken cancellationToken = default);
}
