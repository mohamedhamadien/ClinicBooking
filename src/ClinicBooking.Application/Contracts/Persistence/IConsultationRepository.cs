using ClinicBooking.Domain.Entities;

namespace ClinicBooking.Application.Contracts.Persistence;

public interface IConsultationRepository
{
    Task<ConsultationNote?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ConsultationNote?> GetByAppointmentIdAsync(Guid appointmentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConsultationNote>> GetByPatientIdAsync(Guid tenantId, Guid patientId, CancellationToken cancellationToken = default);
    Task AddAsync(ConsultationNote note, CancellationToken cancellationToken = default);
}
