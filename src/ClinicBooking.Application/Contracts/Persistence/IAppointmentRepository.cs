using ClinicBooking.Domain.Entities;

namespace ClinicBooking.Application.Contracts.Persistence;

public interface IAppointmentRepository
{
    Task<Appointment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Appointment>> GetActiveAppointmentsForDoctorAsync(
        Guid tenantId,
        Guid doctorId,
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Appointment>> GetActiveAppointmentsForPatientAsync(
        Guid tenantId,
        Guid patientId,
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Appointment>> GetMyAppointmentsAsync(
        Guid tenantId,
        Guid patientId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Appointment>> GetAllAppointmentsAsync(
        Guid tenantId,
        DateTime? fromUtc,
        DateTime? toUtc,
        Guid? doctorId,
        CancellationToken cancellationToken = default);
    Task<bool> HasConflictingAppointmentForDoctorAsync(
        Guid tenantId,
        Guid doctorId,
        DateTime startUtc,
        DateTime endUtc,
        Guid? excludeAppointmentId = null,
        CancellationToken cancellationToken = default);
    Task<bool> HasConflictingAppointmentForPatientAsync(
        Guid tenantId,
        Guid patientId,
        DateTime startUtc,
        DateTime endUtc,
        Guid? excludeAppointmentId = null,
        CancellationToken cancellationToken = default);
    Task AddAsync(Appointment appointment, CancellationToken cancellationToken = default);
    Task UpdateAsync(Appointment appointment, CancellationToken cancellationToken = default);
}
