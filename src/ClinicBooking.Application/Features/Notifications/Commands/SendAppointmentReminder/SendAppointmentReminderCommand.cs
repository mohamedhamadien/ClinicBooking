using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Features.Notifications.DTOs;
using ClinicBooking.Domain.Enums;
using MediatR;

namespace ClinicBooking.Application.Features.Notifications.Commands.SendAppointmentReminder;

public sealed record SendAppointmentReminderCommand(
    Guid AppointmentId,
    NotificationChannel Channel = NotificationChannel.WhatsApp,
    string? CustomMessage = null) : IRequest<Result<NotificationLogDto>>;
