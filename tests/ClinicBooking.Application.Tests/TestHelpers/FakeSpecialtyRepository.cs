using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Domain.Entities;

namespace ClinicBooking.Application.Tests.TestHelpers;

public class FakeSpecialtyRepository : ISpecialtyRepository
{
    private readonly List<Specialty> _specialties = [];

    public void Seed(params Specialty[] specialties) => _specialties.AddRange(specialties);

    public Task<Specialty?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_specialties.FirstOrDefault(s => s.Id == id));

    public Task<IReadOnlyList<Specialty>> ListAsync(Guid? tenantId = null, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Specialty>>(_specialties.ToList());

    public Task AddAsync(Specialty specialty, CancellationToken cancellationToken = default)
    {
        _specialties.Add(specialty);
        return Task.CompletedTask;
    }
}
