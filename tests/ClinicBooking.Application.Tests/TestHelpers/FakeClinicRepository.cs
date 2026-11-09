using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Domain.Entities;

namespace ClinicBooking.Application.Tests.TestHelpers;

public class FakeClinicRepository : IClinicRepository
{
    private readonly List<Clinic> _clinics = [];

    public void Seed(params Clinic[] clinics) => _clinics.AddRange(clinics);

    public Task<Clinic?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_clinics.FirstOrDefault(c => c.Id == id));

    public Task<IReadOnlyList<Clinic>> ListAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Clinic>>(_clinics.Where(c => c.TenantId == tenantId).ToList());

    public Task AddAsync(Clinic clinic, CancellationToken cancellationToken = default)
    {
        _clinics.Add(clinic);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Clinic clinic, CancellationToken cancellationToken = default)
    {
        var index = _clinics.FindIndex(c => c.Id == clinic.Id);
        if (index >= 0) _clinics[index] = clinic;
        return Task.CompletedTask;
    }
}
