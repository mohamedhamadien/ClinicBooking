using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Features.Notifications.DTOs;
using ClinicBooking.Domain.Enums;
using MediatR;

namespace ClinicBooking.Application.Features.Notifications.Queries.GetNotificationLogs;

public sealed record GetNotificationLogsQuery(
    int PageNumber = 1,
    int PageSize = 20,
    NotificationChannel? Channel = null,
    NotificationStatus? Status = null,
    string? SearchTerm = null) : IRequest<Result<PagedResult<NotificationLogDto>>>;
