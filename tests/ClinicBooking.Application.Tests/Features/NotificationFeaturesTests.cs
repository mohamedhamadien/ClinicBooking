using ClinicBooking.Application.Features.Notifications.Commands.SendAppointmentReminder;
using ClinicBooking.Application.Features.Notifications.Queries.GetNotificationLogs;
using ClinicBooking.Application.Features.Notifications.Queries.GetWhatsAppLink;
using ClinicBooking.Application.Tests.TestHelpers;
using ClinicBooking.Domain.Entities;
using ClinicBooking.Domain.Enums;
using ClinicBooking.Domain.ValueObjects;
using Xunit;

namespace ClinicBooking.Application.Tests.Features;

public class NotificationFeaturesTests
{
    private readonly FakeAppointmentRepository _appointmentRepo = new();
    private readonly FakePatientRepository _patientRepo = new();
    private readonly FakeDoctorRepository _doctorRepo = new();
    private readonly FakeUserRepository _userRepo = new();
    private readonly FakeClinicRepository _clinicRepo = new();
    private readonly FakeNotificationRepository _notificationRepo = new();
    private readonly FakeUnitOfWork _uow = new();

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _clinicId = Guid.NewGuid();
    private readonly Guid _doctorId = Guid.NewGuid();
    private readonly Guid _patientId = Guid.NewGuid();
    private readonly Guid _doctorUserId = Guid.NewGuid();
    private readonly Guid _patientUserId = Guid.NewGuid();

    private void SetupEntities(out Appointment appt)
    {
        var docUser = User.Create("doctor@clinic.com", "Ahmad", "Hassan", UserRole.Doctor, _tenantId);
        typeof(User).GetProperty(nameof(User.Id))!.SetValue(docUser, _doctorUserId);
        _userRepo.Seed(docUser);

        var patUser = User.Create("patient@example.com", "Sarah", "Ali", UserRole.Patient, _tenantId);
        typeof(User).GetProperty(nameof(User.Id))!.SetValue(patUser, _patientUserId);
        _userRepo.Seed(patUser);

        var doctor = Doctor.Create(_tenantId, _doctorUserId, Guid.NewGuid(), "LIC-999", "Pediatrician");
        typeof(Doctor).GetProperty(nameof(Doctor.Id))!.SetValue(doctor, _doctorId);
        _doctorRepo.Seed(doctor);

        var patient = Patient.Create(_tenantId, _patientUserId, new DateOnly(1995, 5, 20), Gender.Female, PhoneNumber.Create("+201012345678"));
        typeof(Patient).GetProperty(nameof(Patient.Id))!.SetValue(patient, _patientId);
        _patientRepo.Seed(patient);

        var clinic = Clinic.Create(_tenantId, "Health First Clinic", "123 Nile St", "Cairo", "+201012345678");
        typeof(Clinic).GetProperty(nameof(Clinic.Id))!.SetValue(clinic, _clinicId);
        _clinicRepo.Seed(clinic);

        var start = DateTime.UtcNow.AddDays(1);
        var end = start.AddMinutes(30);
        appt = Appointment.Create(_tenantId, _doctorId, _patientId, _clinicId, DateTimeRange.Create(start, end), DateTime.UtcNow);
        _appointmentRepo.Seed(appt);
    }

    [Fact]
    public async Task SendAppointmentReminder_Should_Create_Notification_Log_Successfully()
    {
        // Arrange
        SetupEntities(out var appt);
        var handler = new SendAppointmentReminderCommandHandler(
            _appointmentRepo, _patientRepo, _doctorRepo, _userRepo, _clinicRepo, _notificationRepo, _uow);

        var command = new SendAppointmentReminderCommand(appt.Id, NotificationChannel.WhatsApp);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Sarah Ali", result.Value.RecipientName);
        Assert.Equal("WhatsApp", result.Value.Channel);
        Assert.Equal("Sent", result.Value.Status);
        Assert.Single(_notificationRepo.Items);
    }

    [Fact]
    public async Task GetWhatsAppLink_Should_Return_Encoded_Url_And_Text()
    {
        // Arrange
        SetupEntities(out var appt);
        var handler = new GetWhatsAppLinkQueryHandler(
            _appointmentRepo, _patientRepo, _doctorRepo, _userRepo, _clinicRepo, _notificationRepo, _uow);

        var query = new GetWhatsAppLinkQuery(appt.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Contains("api.whatsapp.com/send?phone=", result.Value.FormattedUrl);
        Assert.Contains("Sarah Ali", result.Value.MessageText);
        Assert.Contains("Health First Clinic", result.Value.MessageText);
        Assert.Single(_notificationRepo.Items);
    }

    [Fact]
    public async Task GetNotificationLogs_Should_Filter_And_Paginate()
    {
        // Arrange
        SetupEntities(out var appt);
        var handler = new SendAppointmentReminderCommandHandler(
            _appointmentRepo, _patientRepo, _doctorRepo, _userRepo, _clinicRepo, _notificationRepo, _uow);

        await handler.Handle(new SendAppointmentReminderCommand(appt.Id, NotificationChannel.WhatsApp), CancellationToken.None);
        await handler.Handle(new SendAppointmentReminderCommand(appt.Id, NotificationChannel.Sms), CancellationToken.None);

        var queryHandler = new GetNotificationLogsQueryHandler(_notificationRepo);
        var query = new GetNotificationLogsQuery(PageNumber: 1, PageSize: 10, Channel: NotificationChannel.WhatsApp);

        // Act
        var result = await queryHandler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalCount);
        Assert.Single(result.Value.Items);
        Assert.Equal("WhatsApp", result.Value.Items.First().Channel);
    }
}
