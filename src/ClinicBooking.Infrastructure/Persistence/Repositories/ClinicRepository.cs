using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClinicBooking.Infrastructure.Persistence.Repositories;

public sealed class ClinicRepository : IClinicRepository
{
    private readonly ClinicBookingDbContext _dbContext;

    public ClinicRepository(ClinicBookingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Clinic?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Clinics.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Clinic>> ListAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Clinics
            .AsNoTracking()
            .Where(c => (tenantId == Guid.Empty || c.TenantId == tenantId) && c.IsActive)
            .OrderBy(c => c.NameEn)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Clinic clinic, CancellationToken cancellationToken = default)
    {
        await _dbContext.Clinics.AddAsync(clinic, cancellationToken);
    }

    public Task UpdateAsync(Clinic clinic, CancellationToken cancellationToken = default)
    {
        _dbContext.Clinics.Update(clinic);
        return Task.CompletedTask;
    }
}
