using ClinicBooking.Domain.Common;
using ClinicBooking.Domain.Enums;

namespace ClinicBooking.Domain.Entities;

public sealed class NotificationLog : Entity<Guid>, IMayHaveTenant
{
    public Guid? TenantId { get; set; }
    public Guid? AppointmentId { get; set; }
    public string RecipientName { get; set; } = string.Empty;
    public string? RecipientPhone { get; set; }
    public string? RecipientEmail { get; set; }
    public NotificationChannel Channel { get; set; }
    public NotificationType Type { get; set; }
    public NotificationStatus Status { get; set; }
    public string? Subject { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime SentAtUtc { get; set; }
    public string? ErrorMessage { get; set; }

    public NotificationLog() : base(Guid.NewGuid()) { }

    public NotificationLog(
        Guid id,
        Guid? tenantId,
        Guid? appointmentId,
        string recipientName,
        string? recipientPhone,
        string? recipientEmail,
        NotificationChannel channel,
        NotificationType type,
        NotificationStatus status,
        string message,
        DateTime sentAtUtc,
        string? subject = null,
        string? errorMessage = null) : base(id)
    {
        TenantId = tenantId;
        AppointmentId = appointmentId;
        RecipientName = recipientName;
        RecipientPhone = recipientPhone;
        RecipientEmail = recipientEmail;
        Channel = channel;
        Type = type;
        Status = status;
        Message = message;
        SentAtUtc = sentAtUtc;
        Subject = subject;
        ErrorMessage = errorMessage;
    }
}
