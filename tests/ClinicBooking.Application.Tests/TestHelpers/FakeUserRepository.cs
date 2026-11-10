using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Domain.Entities;

namespace ClinicBooking.Application.Tests.TestHelpers;

public class FakeUserRepository : IUserRepository
{
    private readonly List<User> _users = [];

    public void Seed(params User[] users) => _users.AddRange(users);

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_users.FirstOrDefault(u => u.Id == id));

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        Task.FromResult(_users.FirstOrDefault(u => u.Email.Value.Equals(email, StringComparison.OrdinalIgnoreCase)));

    public Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default) =>
        Task.FromResult(_users.FirstOrDefault(u => u.RefreshTokens.Any(t => t.Token == refreshToken)));

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        Task.FromResult(_users.Any(u => u.Email.Value.Equals(email, StringComparison.OrdinalIgnoreCase)));

    public Task<IReadOnlyList<User>> GetStaffMembersAsync(Guid? tenantId, Guid? clinicId, CancellationToken cancellationToken = default)
    {
        var result = _users.Where(u => u.Role != Domain.Enums.UserRole.Patient);
        if (tenantId.HasValue) result = result.Where(u => u.TenantId == tenantId.Value);
        if (clinicId.HasValue) result = result.Where(u => u.ClinicId == clinicId.Value);
        return Task.FromResult<IReadOnlyList<User>>(result.ToList());
    }

    public Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        _users.Add(user);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        var index = _users.FindIndex(u => u.Id == user.Id);
        if (index >= 0) _users[index] = user;
        return Task.CompletedTask;
    }
}
