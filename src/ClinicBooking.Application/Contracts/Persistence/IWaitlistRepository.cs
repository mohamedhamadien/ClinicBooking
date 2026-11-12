using ClinicBooking.Domain.Entities;
using ClinicBooking.Domain.Enums;

namespace ClinicBooking.Application.Contracts.Persistence;

public interface IWaitlistRepository
{
    Task<WaitlistEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WaitlistEntry>> ListAsync(
        Guid? clinicId = null,
        Guid? doctorId = null,
        DateOnly? preferredDate = null,
        WaitlistStatus? status = null,
        CancellationToken cancellationToken = default);
    Task<WaitlistEntry?> GetNextWaitingCandidateAsync(
        Guid tenantId,
        Guid clinicId,
        Guid doctorId,
        DateOnly date,
        TimeOnly slotStartTime,
        CancellationToken cancellationToken = default);
    Task AddAsync(WaitlistEntry entry, CancellationToken cancellationToken = default);
    Task UpdateAsync(WaitlistEntry entry, CancellationToken cancellationToken = default);
}
