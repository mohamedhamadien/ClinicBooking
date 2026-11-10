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
            .Include(u => u.Permissions)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var emailAddress = Domain.ValueObjects.EmailAddress.Create(email);
        return _dbContext.Users
            .Include(u => u.RefreshTokens)
            .Include(u => u.Permissions)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == emailAddress, cancellationToken);
    }

    public Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users
            .Include(u => u.RefreshTokens)
            .Include(u => u.Permissions)
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

    public async Task<IReadOnlyList<User>> GetStaffMembersAsync(Guid? tenantId, Guid? clinicId, CancellationToken cancellationToken = default)
    {
        var nonStaffRole = Domain.Enums.UserRole.Patient;
        var query = _dbContext.Users
            .Include(u => u.Permissions)
            .Where(u => u.Role != nonStaffRole);

        if (tenantId.HasValue && tenantId.Value != Guid.Empty)
        {
            query = query.Where(u => u.TenantId == tenantId.Value);
        }

        if (clinicId.HasValue && clinicId.Value != Guid.Empty)
        {
            query = query.Where(u => u.ClinicId == clinicId.Value);
        }

        return await query
            .OrderBy(u => u.Role)
            .ThenBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .ToListAsync(cancellationToken);
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
