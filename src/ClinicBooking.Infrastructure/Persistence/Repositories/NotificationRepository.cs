using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Domain.Entities;
using ClinicBooking.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ClinicBooking.Infrastructure.Persistence.Repositories;

internal sealed class NotificationRepository : INotificationRepository
{
    private readonly ClinicBookingDbContext _dbContext;

    public NotificationRepository(ClinicBookingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(NotificationLog log, CancellationToken cancellationToken = default)
    {
        await _dbContext.NotificationLogs.AddAsync(log, cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationLog>> GetByAppointmentIdAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.NotificationLogs
            .AsNoTracking()
            .Where(n => n.AppointmentId == appointmentId)
            .OrderByDescending(n => n.SentAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<NotificationLog> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        NotificationChannel? channel = null,
        NotificationStatus? status = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.NotificationLogs.AsNoTracking();

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
                                     (n.RecipientEmail != null && n.RecipientEmail.ToLower().Contains(term)) ||
                                     n.Message.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(n => n.SentAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
