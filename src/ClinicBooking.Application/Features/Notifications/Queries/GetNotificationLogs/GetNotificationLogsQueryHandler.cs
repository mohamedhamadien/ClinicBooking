using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Features.Notifications.DTOs;
using MediatR;

namespace ClinicBooking.Application.Features.Notifications.Queries.GetNotificationLogs;

public sealed class GetNotificationLogsQueryHandler : IRequestHandler<GetNotificationLogsQuery, Result<PagedResult<NotificationLogDto>>>
{
    private readonly INotificationRepository _notificationRepository;

    public GetNotificationLogsQueryHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<Result<PagedResult<NotificationLogDto>>> Handle(GetNotificationLogsQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _notificationRepository.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            request.Channel,
            request.Status,
            request.SearchTerm,
            cancellationToken);

        var dtos = items.Select(log => new NotificationLogDto(
            log.Id,
            log.AppointmentId,
            log.RecipientName,
            log.RecipientPhone,
            log.RecipientEmail,
            log.Channel.ToString(),
            log.Type.ToString(),
            log.Status.ToString(),
            log.Subject,
            log.Message,
            log.SentAtUtc,
            log.ErrorMessage)).ToList();

        var pagedResult = new PagedResult<NotificationLogDto>(dtos, totalCount, request.PageNumber, request.PageSize);
        return Result<PagedResult<NotificationLogDto>>.Success(pagedResult);
    }
}
