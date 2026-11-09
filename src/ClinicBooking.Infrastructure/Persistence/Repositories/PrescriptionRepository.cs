using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClinicBooking.Infrastructure.Persistence.Repositories;

public sealed class PrescriptionRepository : IPrescriptionRepository
{
    private readonly ClinicBookingDbContext _dbContext;

    public PrescriptionRepository(ClinicBookingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Prescription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Prescriptions
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Prescription>> GetByPatientIdAsync(Guid tenantId, Guid patientId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Prescriptions
            .Include(p => p.Items)
            .Where(p => p.TenantId == tenantId && p.PatientId == patientId)
            .OrderByDescending(p => p.IssuedAtUtc)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Prescription>> GetByDoctorIdAsync(Guid tenantId, Guid doctorId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Prescriptions
            .Include(p => p.Items)
            .Where(p => p.TenantId == tenantId && p.DoctorId == doctorId)
            .OrderByDescending(p => p.IssuedAtUtc)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Prescription prescription, CancellationToken cancellationToken = default)
    {
        await _dbContext.Prescriptions.AddAsync(prescription, cancellationToken);
    }
}
