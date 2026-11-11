using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Domain.Entities;
using ClinicBooking.Domain.Enums;

namespace ClinicBooking.Application.Tests.TestHelpers;

public sealed class FakeNotificationRepository : INotificationRepository
{
    public List<NotificationLog> Items { get; } = new();

    public Task AddAsync(NotificationLog log, CancellationToken cancellationToken = default)
    {
        Items.Add(log);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<NotificationLog>> GetByAppointmentIdAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<NotificationLog> result = Items.Where(n => n.AppointmentId == appointmentId).OrderByDescending(n => n.SentAtUtc).ToList();
        return Task.FromResult(result);
    }

    public Task<(IReadOnlyList<NotificationLog> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        NotificationChannel? channel = null,
        NotificationStatus? status = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        var query = Items.AsQueryable();

        if (channel.HasValue)
        {
            query = query.Where(n => n.Channel == channel.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(n => n.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(n => n.RecipientName.ToLower().Contains(term) ||
                                     (n.RecipientPhone != null && n.RecipientPhone.Contains(term)) ||
                                     n.Message.ToLower().Contains(term));
        }

        var total = query.Count();
        var paged = query
            .OrderByDescending(n => n.SentAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult(((IReadOnlyList<NotificationLog>)paged, total));
    }
}
