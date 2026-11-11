using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Features.Notifications.DTOs;
using ClinicBooking.Domain.Entities;
using ClinicBooking.Domain.Enums;
using MediatR;

namespace ClinicBooking.Application.Features.Notifications.Commands.SendAppointmentReminder;

public sealed class SendAppointmentReminderCommandHandler : IRequestHandler<SendAppointmentReminderCommand, Result<NotificationLogDto>>
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly IDoctorRepository _doctorRepository;
    private readonly IUserRepository _userRepository;
    private readonly IClinicRepository _clinicRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SendAppointmentReminderCommandHandler(
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

    public async Task<Result<NotificationLogDto>> Handle(SendAppointmentReminderCommand request, CancellationToken cancellationToken)
    {
        var appt = await _appointmentRepository.GetByIdAsync(request.AppointmentId, cancellationToken);
        if (appt is null)
        {
            return Result.Failure<NotificationLogDto>(Error.NotFound("Appointment", request.AppointmentId));
        }

        var patient = await _patientRepository.GetByIdAsync(appt.PatientId, cancellationToken);
        var patientUser = patient is not null ? await _userRepository.GetByIdAsync(patient.UserId, cancellationToken) : null;
        var patientName = patientUser is not null ? $"{patientUser.FirstName} {patientUser.LastName}".Trim() : "Patient";
        var patientPhone = patient?.EmergencyContact?.Value;
        var patientEmail = patientUser?.Email?.Value;

        var doctor = await _doctorRepository.GetByIdAsync(appt.DoctorId, cancellationToken);
        var doctorUser = doctor is not null ? await _userRepository.GetByIdAsync(doctor.UserId, cancellationToken) : null;
        var doctorName = doctorUser is not null ? $"{doctorUser.FirstName} {doctorUser.LastName}".Trim() : "Doctor";

        var clinic = await _clinicRepository.GetByIdAsync(appt.ClinicId, cancellationToken);
        var clinicName = clinic?.Name ?? "Clinic";

        var timeStr = appt.StartTimeUtc.ToString("yyyy-MM-dd HH:mm UTC");
        var message = !string.IsNullOrWhiteSpace(request.CustomMessage)
            ? request.CustomMessage
            : $"مرحباً {patientName}، نذكرك بموعدك الطبي القادم مع د. {doctorName} في {clinicName} بتاريخ {timeStr}.\n\nDear {patientName}, this is a reminder for your upcoming appointment with Dr. {doctorName} at {clinicName} on {timeStr}.";

        var subject = $"Appointment Reminder - {clinicName}";

        var status = NotificationStatus.Sent;
        string? errorMessage = null;

        if (request.Channel == NotificationChannel.WhatsApp && string.IsNullOrWhiteSpace(patientPhone))
        {
            status = NotificationStatus.Failed;
            errorMessage = "Patient phone number is not available for WhatsApp.";
        }
        else if (request.Channel == NotificationChannel.Email && string.IsNullOrWhiteSpace(patientEmail))
        {
            status = NotificationStatus.Failed;
            errorMessage = "Patient email is not available.";
        }

        var log = new NotificationLog(
            Guid.NewGuid(),
            appt.TenantId,
            appt.Id,
            patientName,
            patientPhone,
            patientEmail,
            request.Channel,
            NotificationType.AppointmentReminder,
            status,
            message,
            DateTime.UtcNow,
            subject,
            errorMessage);

        await _notificationRepository.AddAsync(log, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new NotificationLogDto(
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
            log.ErrorMessage);

        return Result.Success(dto);
    }
}
