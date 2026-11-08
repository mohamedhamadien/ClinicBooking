using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClinicBooking.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly ClinicBookingDbContext _dbContext;

    public UserRepository(ClinicBookingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users
            .Include(u => u.RefreshTokens)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var emailAddress = Domain.ValueObjects.EmailAddress.Create(email);
        return _dbContext.Users
            .Include(u => u.RefreshTokens)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == emailAddress, cancellationToken);
    }

    public Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users
            .Include(u => u.RefreshTokens)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == refreshToken), cancellationToken);
    }

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var emailAddress = Domain.ValueObjects.EmailAddress.Create(email);
        return _dbContext.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == emailAddress, cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _dbContext.Users.AddAsync(user, cancellationToken);
    }

    public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        _dbContext.Users.Update(user);
        return Task.CompletedTask;
    }
}
