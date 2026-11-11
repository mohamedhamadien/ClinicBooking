using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Domain.Entities;
using ClinicBooking.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ClinicBooking.Infrastructure.Persistence.Repositories;

public sealed class AppointmentRepository : IAppointmentRepository
{
    private static readonly AppointmentStatus[] InactiveStatuses =
    [
        AppointmentStatus.CancelledByPatient,
        AppointmentStatus.CancelledByDoctor,
        AppointmentStatus.CancelledByAdmin,
        AppointmentStatus.Rejected
    ];

    private readonly ClinicBookingDbContext _dbContext;

    public AppointmentRepository(ClinicBookingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Appointment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Appointments.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Appointment>> GetActiveAppointmentsForDoctorAsync(
        Guid tenantId,
        Guid doctorId,
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Appointments
            .Where(a => a.TenantId == tenantId &&
                        a.DoctorId == doctorId &&
                        !InactiveStatuses.Contains(a.Status) &&
                        a.StartTimeUtc < endUtc &&
                        a.EndTimeUtc > startUtc)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Appointment>> GetActiveAppointmentsForPatientAsync(
        Guid tenantId,
        Guid patientId,
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Appointments
            .Where(a => a.TenantId == tenantId &&
                        a.PatientId == patientId &&
                        !InactiveStatuses.Contains(a.Status) &&
                        a.StartTimeUtc < endUtc &&
                        a.EndTimeUtc > startUtc)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Appointment>> GetMyAppointmentsAsync(
        Guid tenantId,
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Appointments
            .Where(a => a.TenantId == tenantId && a.PatientId == patientId)
            .OrderByDescending(a => a.StartTimeUtc)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Appointment>> GetAllAppointmentsAsync(
        Guid tenantId,
        DateTime? fromUtc,
        DateTime? toUtc,
        Guid? doctorId,
        Guid? clinicId = null,
        CancellationToken cancellationToken = default)
    {
        var query = tenantId == Guid.Empty
            ? _dbContext.Appointments.AsQueryable()
            : _dbContext.Appointments.Where(a => a.TenantId == tenantId);

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
            query = query.Where(a => a.StartTimeUtc >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(a => a.EndTimeUtc <= toUtc.Value);
        }

        return await query
            .OrderByDescending(a => a.StartTimeUtc)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public Task<bool> HasConflictingAppointmentForDoctorAsync(
        Guid tenantId,
        Guid doctorId,
        DateTime startUtc,
        DateTime endUtc,
        Guid? excludeAppointmentId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Appointments
            .Where(a => a.TenantId == tenantId &&
                        a.DoctorId == doctorId &&
                        !InactiveStatuses.Contains(a.Status) &&
                        a.StartTimeUtc < endUtc &&
                        a.EndTimeUtc > startUtc);

        if (excludeAppointmentId.HasValue)
        {
            query = query.Where(a => a.Id != excludeAppointmentId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public Task<bool> HasConflictingAppointmentForPatientAsync(
        Guid tenantId,
        Guid patientId,
        DateTime startUtc,
        DateTime endUtc,
        Guid? excludeAppointmentId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Appointments
            .Where(a => a.TenantId == tenantId &&
                        a.PatientId == patientId &&
                        !InactiveStatuses.Contains(a.Status) &&
                        a.StartTimeUtc < endUtc &&
                        a.EndTimeUtc > startUtc);

        if (excludeAppointmentId.HasValue)
        {
            query = query.Where(a => a.Id != excludeAppointmentId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        await _dbContext.Appointments.AddAsync(appointment, cancellationToken);
    }

    public Task UpdateAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        _dbContext.Appointments.Update(appointment);
        return Task.CompletedTask;
    }
}
