using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClinicBooking.Infrastructure.Persistence.Repositories;

public sealed class DoctorRepository : IDoctorRepository
{
    private readonly ClinicBookingDbContext _dbContext;

    public DoctorRepository(ClinicBookingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Doctor?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Doctors.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public Task<Doctor?> GetByIdWithSchedulesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Doctors
            .Include(d => d.Availabilities)
            .Include(d => d.Exceptions)
            .Include(d => d.DoctorClinics)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public Task<Doctor?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Doctors
            .Include(d => d.Availabilities)
            .Include(d => d.Exceptions)
            .Include(d => d.DoctorClinics)
            .FirstOrDefaultAsync(d => d.UserId == userId, cancellationToken);
    }

    public async Task<IReadOnlyList<Doctor>> ListAsync(
        Guid tenantId,
        Guid? specialtyId = null,
        Guid? clinicId = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Doctors
            .Include(d => d.Availabilities.Where(a => a.IsActive))
            .Where(d => d.TenantId == tenantId && d.IsActive);

        if (specialtyId.HasValue)
        {
            query = query.Where(d => d.SpecialtyId == specialtyId.Value);
        }

        if (clinicId.HasValue)
        {
            query = query.Where(d => d.Availabilities.Any(a => a.ClinicId == clinicId.Value && a.IsActive)
                                  || d.DoctorClinics.Any(dc => dc.ClinicId == clinicId.Value));
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(d => d.LicenseNumber.ToLower().Contains(term)
                                  || d.BioEn.ToLower().Contains(term)
                                  || d.BioAr.ToLower().Contains(term));
        }

        return await query
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Doctor doctor, CancellationToken cancellationToken = default)
    {
        await _dbContext.Doctors.AddAsync(doctor, cancellationToken);
    }

    public Task UpdateAsync(Doctor doctor, CancellationToken cancellationToken = default)
    {
        _dbContext.Doctors.Update(doctor);
        return Task.CompletedTask;
    }
}
