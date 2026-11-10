using ClinicBooking.Domain.Entities;

namespace ClinicBooking.Application.Contracts.Persistence;

public interface IInsuranceProviderRepository
{
    Task<InsuranceProvider?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InsuranceProvider>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(InsuranceProvider provider, CancellationToken cancellationToken = default);
}
