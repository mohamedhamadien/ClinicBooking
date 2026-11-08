using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Domain.Entities;

namespace ClinicBooking.Application.Tests.TestHelpers;

public class FakePatientRepository : IPatientRepository
{
    private readonly List<Patient> _patients = [];

    public void Seed(params Patient[] patients) => _patients.AddRange(patients);

    public Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_patients.FirstOrDefault(p => p.Id == id));

    public Task<Patient?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_patients.FirstOrDefault(p => p.UserId == userId));

    public Task AddAsync(Patient patient, CancellationToken cancellationToken = default)
    {
        _patients.Add(patient);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Patient patient, CancellationToken cancellationToken = default)
    {
        var index = _patients.FindIndex(p => p.Id == patient.Id);
        if (index >= 0)
        {
            _patients[index] = patient;
        }

        return Task.CompletedTask;
    }
}
