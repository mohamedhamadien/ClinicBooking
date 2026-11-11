namespace ClinicBooking.Application.Features.Notifications.DTOs;

public sealed record NotificationLogDto(
    Guid Id,
    Guid? AppointmentId,
    string RecipientName,
    string? RecipientPhone,
    string? RecipientEmail,
    string Channel,
    string Type,
    string Status,
    string? Subject,
    string Message,
    DateTime SentAtUtc,
    string? ErrorMessage);
