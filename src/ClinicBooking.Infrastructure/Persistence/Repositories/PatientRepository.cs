using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClinicBooking.Infrastructure.Persistence.Repositories;

public sealed class PatientRepository : IPatientRepository
{
    private readonly ClinicBookingDbContext _dbContext;

    public PatientRepository(ClinicBookingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Patients.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public Task<Patient?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Patients.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
    }

    public async Task<IReadOnlyList<Patient>> ListAsync(Guid tenantId, string? searchTerm = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Patients
            .AsNoTracking()
            .Where(p => tenantId == Guid.Empty || p.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            var matchingUserIds = await _dbContext.Users
                .AsNoTracking()
                .Where(u => (tenantId == Guid.Empty || u.TenantId == tenantId) &&
                            (u.FirstName.ToLower().Contains(term) ||
                             u.LastName.ToLower().Contains(term) ||
                             ((string)u.Email).ToLower().Contains(term)))
                .Select(u => u.Id)
                .ToListAsync(cancellationToken);

            query = query.Where(p => matchingUserIds.Contains(p.UserId) ||
                                     (p.EmergencyContact != null && ((string)p.EmergencyContact).Contains(term)) ||
                                     (p.BloodGroup != null && p.BloodGroup.ToLower().Contains(term)) ||
                                     (p.Allergies != null && p.Allergies.ToLower().Contains(term)) ||
                                     (p.ChronicConditions != null && p.ChronicConditions.ToLower().Contains(term)));
        }

        return await query.OrderByDescending(p => p.CreatedAtUtc).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Patient patient, CancellationToken cancellationToken = default)
    {
        await _dbContext.Patients.AddAsync(patient, cancellationToken);
    }

    public Task UpdateAsync(Patient patient, CancellationToken cancellationToken = default)
    {
        _dbContext.Patients.Update(patient);
        return Task.CompletedTask;
    }
}
