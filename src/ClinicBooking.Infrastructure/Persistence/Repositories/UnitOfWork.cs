using System.Data;
using ClinicBooking.Application.Common.Exceptions;
using ClinicBooking.Application.Contracts.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ClinicBooking.Infrastructure.Persistence.Repositories;

public sealed class UnitOfWork : IUnitOfWork
{
    private static readonly SemaphoreSlim _inMemoryBookingSemaphore = new(1, 1);
    private readonly ClinicBookingDbContext _dbContext;

    public UnitOfWork(ClinicBookingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<T> ExecuteInSerializableTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        if (_dbContext.Database.ProviderName?.Contains("InMemory") == true)
        {
            await _inMemoryBookingSemaphore.WaitAsync(cancellationToken);
            try
            {
                return await operation(cancellationToken);
            }
            finally
            {
                _inMemoryBookingSemaphore.Release();
            }
        }

        if (_dbContext.Database.CurrentTransaction is not null)
        {
            return await operation(cancellationToken);
        }

        var strategy = _dbContext.Database.CreateExecutionStrategy();
        try
        {
            return await strategy.ExecuteAsync(async ct =>
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
                var result = await operation(ct);
                await transaction.CommitAsync(ct);
                return result;
            }, cancellationToken);
        }
        catch (SqlException sqlEx) when (sqlEx.Number is 1205 or 3960 or 2601 or 2627)
        {
            throw new ConcurrencyConflictException(
                "Appointment.DoctorSlotUnavailable",
                "The requested appointment slot was concurrently reserved by another request.",
                sqlEx);
        }
    }
}
