using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClinicBooking.Infrastructure.Persistence.Repositories;

public sealed class InsuranceProviderRepository : IInsuranceProviderRepository
{
    private readonly ClinicBookingDbContext _dbContext;

    public InsuranceProviderRepository(ClinicBookingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<InsuranceProvider?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.InsuranceProviders.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<InsuranceProvider>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.InsuranceProviders
            .OrderBy(p => p.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(InsuranceProvider provider, CancellationToken cancellationToken = default)
    {
        await _dbContext.InsuranceProviders.AddAsync(provider, cancellationToken);
    }
}
