using ClinicBooking.Domain.Entities;

namespace ClinicBooking.Application.Contracts.Persistence;

public interface IPatientVitalSignsRepository
{
    Task<IReadOnlyList<PatientVitalSigns>> GetByPatientIdAsync(Guid tenantId, Guid patientId, CancellationToken cancellationToken = default);
    Task<PatientVitalSigns?> GetByConsultationIdAsync(Guid consultationId, CancellationToken cancellationToken = default);
    Task AddAsync(PatientVitalSigns vitalSigns, CancellationToken cancellationToken = default);
}
