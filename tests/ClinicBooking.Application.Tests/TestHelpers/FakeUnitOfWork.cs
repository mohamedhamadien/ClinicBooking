using ClinicBooking.Application.Contracts.Persistence;

namespace ClinicBooking.Application.Tests.TestHelpers;

public class FakeUnitOfWork : IUnitOfWork
{
    public int SaveChangesCallsCount { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveChangesCallsCount++;
        return Task.FromResult(1);
    }
}
