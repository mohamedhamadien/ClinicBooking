using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Domain.Entities;

namespace ClinicBooking.Application.Tests.TestHelpers;

public class FakeDoctorRepository : IDoctorRepository
{
    private readonly List<Doctor> _doctors = [];

    public void Seed(params Doctor[] doctors) => _doctors.AddRange(doctors);

    public Task<Doctor?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_doctors.FirstOrDefault(d => d.Id == id));

    public Task<Doctor?> GetByIdWithSchedulesAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_doctors.FirstOrDefault(d => d.Id == id));

    public Task<Doctor?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_doctors.FirstOrDefault(d => d.UserId == userId));

    public Task<IReadOnlyList<Doctor>> ListAsync(
        Guid tenantId,
        Guid? specialtyId = null,
        Guid? clinicId = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        var query = _doctors.Where(d => d.TenantId == tenantId && d.IsActive);

        if (specialtyId.HasValue)
        {
            query = query.Where(d => d.SpecialtyId == specialtyId.Value);
        }

        return Task.FromResult<IReadOnlyList<Doctor>>(query.ToList());
    }

    public Task AddAsync(Doctor doctor, CancellationToken cancellationToken = default)
    {
        _doctors.Add(doctor);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Doctor doctor, CancellationToken cancellationToken = default)
    {
        var index = _doctors.FindIndex(d => d.Id == doctor.Id);
        if (index >= 0)
        {
            _doctors[index] = doctor;
        }

        return Task.CompletedTask;
    }
}
