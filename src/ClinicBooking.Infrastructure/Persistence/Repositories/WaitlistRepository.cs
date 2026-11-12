using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Domain.Entities;
using ClinicBooking.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ClinicBooking.Infrastructure.Persistence.Repositories;

internal sealed class WaitlistRepository : IWaitlistRepository
{
    private readonly ClinicBookingDbContext _dbContext;

    public WaitlistRepository(ClinicBookingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<WaitlistEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.WaitlistEntries
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<WaitlistEntry>> ListAsync(
        Guid? clinicId = null,
        Guid? doctorId = null,
        DateOnly? preferredDate = null,
        WaitlistStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.WaitlistEntries.AsNoTracking().AsQueryable();

        if (clinicId.HasValue && clinicId.Value != Guid.Empty)
            query = query.Where(w => w.ClinicId == clinicId.Value);

        if (doctorId.HasValue && doctorId.Value != Guid.Empty)
            query = query.Where(w => w.DoctorId == doctorId.Value);

        if (preferredDate.HasValue)
            query = query.Where(w => w.PreferredDate == preferredDate.Value);

        if (status.HasValue)
            query = query.Where(w => w.Status == status.Value);

        return await query
            .OrderByDescending(w => w.Priority)
            .ThenBy(w => w.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<WaitlistEntry?> GetNextWaitingCandidateAsync(
        Guid tenantId,
        Guid clinicId,
        Guid doctorId,
        DateOnly date,
        TimeOnly slotStartTime,
        CancellationToken cancellationToken = default)
    {
        var candidates = await _dbContext.WaitlistEntries
            .Where(w => w.TenantId == tenantId
                     && w.ClinicId == clinicId
                     && w.DoctorId == doctorId
                     && w.PreferredDate == date
                     && w.Status == WaitlistStatus.Waiting)
            .OrderByDescending(w => w.Priority)
            .ThenBy(w => w.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return candidates.FirstOrDefault(w =>
            (!w.PreferredStartTime.HasValue || slotStartTime >= w.PreferredStartTime.Value) &&
            (!w.PreferredEndTime.HasValue || slotStartTime <= w.PreferredEndTime.Value));
    }

    public async Task AddAsync(WaitlistEntry entry, CancellationToken cancellationToken = default)
    {
        await _dbContext.WaitlistEntries.AddAsync(entry, cancellationToken);
    }

    public Task UpdateAsync(WaitlistEntry entry, CancellationToken cancellationToken = default)
    {
        _dbContext.WaitlistEntries.Update(entry);
        return Task.CompletedTask;
    }
}
