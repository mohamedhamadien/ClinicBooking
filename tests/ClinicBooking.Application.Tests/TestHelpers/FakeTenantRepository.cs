using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Domain.Entities;

namespace ClinicBooking.Application.Tests.TestHelpers;

public class FakeTenantRepository : ITenantRepository
{
    private readonly List<Tenant> _tenants = [];

    public void Seed(params Tenant[] tenants) => _tenants.AddRange(tenants);

    public Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_tenants.FirstOrDefault(t => t.Id == id));

    public Task<Tenant?> GetByIdentifierAsync(string identifier, CancellationToken cancellationToken = default) =>
        Task.FromResult(_tenants.FirstOrDefault(t => t.Identifier.Equals(identifier, StringComparison.OrdinalIgnoreCase)));

    public Task<bool> ExistsAsync(string identifier, CancellationToken cancellationToken = default) =>
        Task.FromResult(_tenants.Any(t => t.Identifier.Equals(identifier, StringComparison.OrdinalIgnoreCase)));

    public Task<IReadOnlyList<Tenant>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Tenant>>(_tenants.AsReadOnly());

    public Task AddAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        _tenants.Add(tenant);
        return Task.CompletedTask;
    }
}
