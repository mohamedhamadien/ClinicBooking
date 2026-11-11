using System.Text.RegularExpressions;
using System.Web;
using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Features.Notifications.DTOs;
using ClinicBooking.Domain.Entities;
using ClinicBooking.Domain.Enums;
using MediatR;

namespace ClinicBooking.Application.Features.Notifications.Queries.GetWhatsAppLink;

public sealed partial class GetWhatsAppLinkQueryHandler : IRequestHandler<GetWhatsAppLinkQuery, Result<WhatsAppReminderLinkDto>>
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly IDoctorRepository _doctorRepository;
    private readonly IUserRepository _userRepository;
    private readonly IClinicRepository _clinicRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public GetWhatsAppLinkQueryHandler(
        IAppointmentRepository appointmentRepository,
        IPatientRepository patientRepository,
        IDoctorRepository doctorRepository,
        IUserRepository userRepository,
        IClinicRepository clinicRepository,
        INotificationRepository notificationRepository,
        IUnitOfWork unitOfWork)
    {
        _appointmentRepository = appointmentRepository;
        _patientRepository = patientRepository;
        _doctorRepository = doctorRepository;
        _userRepository = userRepository;
        _clinicRepository = clinicRepository;
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<WhatsAppReminderLinkDto>> Handle(GetWhatsAppLinkQuery request, CancellationToken cancellationToken)
    {
        var appt = await _appointmentRepository.GetByIdAsync(request.AppointmentId, cancellationToken);
        if (appt is null)
        {
            return Result.Failure<WhatsAppReminderLinkDto>(Error.NotFound("Appointment", request.AppointmentId));
        }

        var patient = await _patientRepository.GetByIdAsync(appt.PatientId, cancellationToken);
        var patientUser = patient is not null ? await _userRepository.GetByIdAsync(patient.UserId, cancellationToken) : null;
        var rawPhone = patient?.EmergencyContact?.Value;

        if (string.IsNullOrWhiteSpace(rawPhone))
        {
            return Result.Failure<WhatsAppReminderLinkDto>(Error.Validation("Patient.NoPhone", "Patient phone number is not available for WhatsApp."));
        }

        // Clean phone number (strip spaces, dashes, parentheses, plus)
        var cleanPhone = PhoneSanitizerRegex().Replace(rawPhone, "");
        if (cleanPhone.StartsWith("00"))
        {
            cleanPhone = cleanPhone[2..];
        }

        var patientName = patientUser is not null ? $"{patientUser.FirstName} {patientUser.LastName}".Trim() : "Patient";
        var doctor = await _doctorRepository.GetByIdAsync(appt.DoctorId, cancellationToken);
        var doctorUser = doctor is not null ? await _userRepository.GetByIdAsync(doctor.UserId, cancellationToken) : null;
        var doctorName = doctorUser is not null ? $"{doctorUser.FirstName} {doctorUser.LastName}".Trim() : "Doctor";
        var clinic = await _clinicRepository.GetByIdAsync(appt.ClinicId, cancellationToken);
        var clinicName = clinic?.Name ?? "Clinic";

        var dateStr = appt.StartTimeUtc.ToString("yyyy-MM-dd");
        var timeStr = appt.StartTimeUtc.ToString("hh:mm tt UTC");

        var text = $"مرحباً {patientName} 🌸\nنود تذكيركم بموعدكم الطبي لدى {clinicName}:\n👨‍⚕️ الطبيب: د. {doctorName}\n📅 التاريخ: {dateStr}\n⏰ الوقت: {timeStr}\n\nنتمنى لكم دوام الصحة والعافية! للاستفسار أو التعديل، يُرجى التواصل معنا.\n\nHello {patientName}, this is a reminder for your appointment at {clinicName} with Dr. {doctorName} on {dateStr} at {timeStr}.";

        var encodedText = HttpUtility.UrlEncode(text);
        var url = $"https://api.whatsapp.com/send?phone={cleanPhone}&text={encodedText}";

        // Record a notification log entry for audit trail
        var log = new NotificationLog(
            Guid.NewGuid(),
            appt.TenantId,
            appt.Id,
            patientName,
            rawPhone,
            patientUser?.Email?.Value,
            NotificationChannel.WhatsApp,
            NotificationType.AppointmentReminder,
            NotificationStatus.Delivered,
            text,
            DateTime.UtcNow,
            "WhatsApp Appointment Reminder Link Generated",
            null);

        await _notificationRepository.AddAsync(log, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new WhatsAppReminderLinkDto(
            appt.Id,
            patientName,
            cleanPhone,
            url,
            text));
    }

    [GeneratedRegex(@"[^\d]")]
    private static partial Regex PhoneSanitizerRegex();
}
