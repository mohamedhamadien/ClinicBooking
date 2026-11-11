using ClinicBooking.Domain.Entities;
using ClinicBooking.Domain.Enums;

namespace ClinicBooking.Application.Contracts.Persistence;

public interface INotificationRepository
{
    Task AddAsync(NotificationLog log, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationLog>> GetByAppointmentIdAsync(Guid appointmentId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<NotificationLog> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        NotificationChannel? channel = null,
        NotificationStatus? status = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default);
}
