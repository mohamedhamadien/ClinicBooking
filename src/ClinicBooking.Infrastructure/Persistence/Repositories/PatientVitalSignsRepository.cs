using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClinicBooking.Infrastructure.Persistence.Repositories;

public sealed class PatientVitalSignsRepository : IPatientVitalSignsRepository
{
    private readonly ClinicBookingDbContext _dbContext;

    public PatientVitalSignsRepository(ClinicBookingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<PatientVitalSigns>> GetByPatientIdAsync(Guid tenantId, Guid patientId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PatientVitalSigns
            .Where(v => v.TenantId == tenantId && v.PatientId == patientId)
            .OrderByDescending(v => v.RecordedAtUtc)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public Task<PatientVitalSigns?> GetByConsultationIdAsync(Guid consultationId, CancellationToken cancellationToken = default)
    {
        return _dbContext.PatientVitalSigns
            .FirstOrDefaultAsync(v => v.ConsultationId == consultationId, cancellationToken);
    }

    public async Task AddAsync(PatientVitalSigns vitalSigns, CancellationToken cancellationToken = default)
    {
        await _dbContext.PatientVitalSigns.AddAsync(vitalSigns, cancellationToken);
    }
}
