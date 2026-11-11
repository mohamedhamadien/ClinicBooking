namespace ClinicBooking.Application.Features.Notifications.DTOs;

public sealed record WhatsAppReminderLinkDto(
    Guid AppointmentId,
    string RecipientName,
    string RecipientPhone,
    string FormattedUrl,
    string MessageText);
