using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Domain.Entities;
using ClinicBooking.Domain.ValueObjects;

namespace ClinicBooking.Application.Tests.TestHelpers;

public class FakeAppointmentRepository : IAppointmentRepository
{
    private readonly List<Appointment> _appointments = [];

    public void Seed(params Appointment[] appointments) => _appointments.AddRange(appointments);

    public Task<Appointment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_appointments.FirstOrDefault(a => a.Id == id));

    public Task<IReadOnlyList<Appointment>> GetActiveAppointmentsForDoctorAsync(
        Guid tenantId,
        Guid doctorId,
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken = default)
    {
        var range = DateTimeRange.Create(startUtc, endUtc);
        var active = _appointments
            .Where(a => a.TenantId == tenantId &&
                        a.DoctorId == doctorId &&
                        a.IsSlotOccupied &&
                        a.ScheduledRange.OverlapsWith(range))
            .ToList();

        return Task.FromResult<IReadOnlyList<Appointment>>(active);
    }

    public Task<IReadOnlyList<Appointment>> GetActiveAppointmentsForPatientAsync(
        Guid tenantId,
        Guid patientId,
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken = default)
    {
        var range = DateTimeRange.Create(startUtc, endUtc);
        var active = _appointments
            .Where(a => a.TenantId == tenantId &&
                        a.PatientId == patientId &&
                        a.IsSlotOccupied &&
                        a.ScheduledRange.OverlapsWith(range))
            .ToList();

        return Task.FromResult<IReadOnlyList<Appointment>>(active);
    }

    public Task<IReadOnlyList<Appointment>> GetMyAppointmentsAsync(
        Guid tenantId,
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        var items = _appointments
            .Where(a => a.TenantId == tenantId && a.PatientId == patientId)
            .OrderByDescending(a => a.ScheduledRange.StartUtc)
            .ToList();

        return Task.FromResult<IReadOnlyList<Appointment>>(items);
    }

    public Task<IReadOnlyList<Appointment>> GetAllAppointmentsAsync(
        Guid tenantId,
        DateTime? fromUtc,
        DateTime? toUtc,
        Guid? doctorId,
        Guid? clinicId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _appointments.Where(a => a.TenantId == tenantId);

        if (clinicId.HasValue)
        {
            query = query.Where(a => a.ClinicId == clinicId.Value);
        }

        if (doctorId.HasValue)
        {
            query = query.Where(a => a.DoctorId == doctorId.Value);
        }

        if (fromUtc.HasValue)
        {
            query = query.Where(a => a.ScheduledRange.StartUtc >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(a => a.ScheduledRange.EndUtc <= toUtc.Value);
        }

        return Task.FromResult<IReadOnlyList<Appointment>>(query.ToList());
    }

    public Task<bool> HasConflictingAppointmentForDoctorAsync(
        Guid tenantId,
        Guid doctorId,
        DateTime startUtc,
        DateTime endUtc,
        Guid? excludeAppointmentId = null,
        CancellationToken cancellationToken = default)
    {
        var range = DateTimeRange.Create(startUtc, endUtc);
        var hasConflict = _appointments.Any(a =>
            a.TenantId == tenantId &&
            a.DoctorId == doctorId &&
            a.IsSlotOccupied &&
            (excludeAppointmentId == null || a.Id != excludeAppointmentId.Value) &&
            a.ScheduledRange.OverlapsWith(range));

        return Task.FromResult(hasConflict);
    }

    public Task<bool> HasConflictingAppointmentForPatientAsync(
        Guid tenantId,
        Guid patientId,
        DateTime startUtc,
        DateTime endUtc,
        Guid? excludeAppointmentId = null,
        CancellationToken cancellationToken = default)
    {
        var range = DateTimeRange.Create(startUtc, endUtc);
        var hasConflict = _appointments.Any(a =>
            a.TenantId == tenantId &&
            a.PatientId == patientId &&
            a.IsSlotOccupied &&
            (excludeAppointmentId == null || a.Id != excludeAppointmentId.Value) &&
            a.ScheduledRange.OverlapsWith(range));

        return Task.FromResult(hasConflict);
    }

    public Task AddAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        _appointments.Add(appointment);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        var index = _appointments.FindIndex(a => a.Id == appointment.Id);
        if (index >= 0)
        {
            _appointments[index] = appointment;
        }

        return Task.CompletedTask;
    }
}
