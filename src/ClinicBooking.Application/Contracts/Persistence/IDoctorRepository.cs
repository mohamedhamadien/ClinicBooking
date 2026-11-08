using ClinicBooking.Domain.Entities;

namespace ClinicBooking.Application.Contracts.Persistence;

public interface IDoctorRepository
{
    Task<Doctor?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Doctor?> GetByIdWithSchedulesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Doctor?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Doctor>> ListAsync(
        Guid tenantId,
        Guid? specialtyId = null,
        Guid? clinicId = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default);
    Task AddAsync(Doctor doctor, CancellationToken cancellationToken = default);
    Task UpdateAsync(Doctor doctor, CancellationToken cancellationToken = default);
}
