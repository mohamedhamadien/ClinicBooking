using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClinicBooking.Infrastructure.Persistence.Repositories;

public sealed class SpecialtyRepository : ISpecialtyRepository
{
    private readonly ClinicBookingDbContext _dbContext;

    public SpecialtyRepository(ClinicBookingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Specialty?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Specialties.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Specialty>> ListAsync(Guid? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Specialties.AsNoTracking().Where(s => s.IsActive);

        if (tenantId.HasValue)
        {
            query = query.Where(s => s.TenantId == null || s.TenantId == tenantId.Value);
        }

        return await query
            .OrderBy(s => s.NameEn)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Specialty specialty, CancellationToken cancellationToken = default)
    {
        await _dbContext.Specialties.AddAsync(specialty, cancellationToken);
    }
}
