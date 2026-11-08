using ClinicBooking.Application.Contracts.Persistence;

namespace ClinicBooking.Infrastructure.Persistence.Repositories;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ClinicBookingDbContext _dbContext;

    public UnitOfWork(ClinicBookingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
