using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClinicBooking.Infrastructure.Persistence.Repositories;

public sealed class ConsultationRepository : IConsultationRepository
{
    private readonly ClinicBookingDbContext _dbContext;

    public ConsultationRepository(ClinicBookingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ConsultationNote?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.ConsultationNotes.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public Task<ConsultationNote?> GetByAppointmentIdAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        return _dbContext.ConsultationNotes.FirstOrDefaultAsync(c => c.AppointmentId == appointmentId, cancellationToken);
    }

    public async Task<IReadOnlyList<ConsultationNote>> GetByPatientIdAsync(Guid tenantId, Guid patientId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ConsultationNotes
            .Where(c => c.TenantId == tenantId && c.PatientId == patientId)
            .OrderByDescending(c => c.ConsultationDateUtc)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ConsultationNote note, CancellationToken cancellationToken = default)
    {
        await _dbContext.ConsultationNotes.AddAsync(note, cancellationToken);
    }
}
