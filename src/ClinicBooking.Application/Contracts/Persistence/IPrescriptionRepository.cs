using ClinicBooking.Domain.Entities;

namespace ClinicBooking.Application.Contracts.Persistence;

public interface IPrescriptionRepository
{
    Task<Prescription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Prescription>> GetByPatientIdAsync(Guid tenantId, Guid patientId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Prescription>> GetByDoctorIdAsync(Guid tenantId, Guid doctorId, CancellationToken cancellationToken = default);
    Task AddAsync(Prescription prescription, CancellationToken cancellationToken = default);
}
