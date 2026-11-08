using System.Globalization;
using ClinicBooking.Application.Common.Localization;
using ClinicBooking.Application.Features.Appointments.Commands.BookAppointment;
using ClinicBooking.Application.Features.Appointments.Commands.CancelAppointment;
using ClinicBooking.Application.Tests.TestHelpers;
using ClinicBooking.Domain.Entities;
using ClinicBooking.Domain.Enums;
using ClinicBooking.Domain.ValueObjects;

namespace ClinicBooking.Application.Tests.Localization;

public class LocalizedAppointmentHandlingTests
{
    private readonly FakeAppointmentRepository _appointmentRepository = new();
    private readonly FakeDoctorRepository _doctorRepository = new();
    private readonly FakePatientRepository _patientRepository = new();
    private readonly FakeTenantRepository _tenantRepository = new();
    private readonly FakeDateTimeProvider _dateTimeProvider = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _doctorId = Guid.NewGuid();
    private readonly Guid _patientId = Guid.NewGuid();
    private readonly Guid _clinicId = Guid.NewGuid();

    public LocalizedAppointmentHandlingTests()
    {
        var tenant = Tenant.Create("مستشفى الأمل", "amal-hospital", cancellationNoticeHours: 24, requiresDoctorApproval: false, nameAr: "مستشفى الأمل التخصصي");
        typeof(Tenant).GetProperty(nameof(Tenant.Id))!.SetValue(tenant, _tenantId);
        _tenantRepository.Seed(tenant);

        var doctor = Doctor.Create(_tenantId, Guid.NewGuid(), Guid.NewGuid(), "LIC-888", "Cardiologist", defaultSlotDurationMinutes: 30, requiresApproval: false, bioAr: "طبيب قلب");
        typeof(Doctor).GetProperty(nameof(Doctor.Id))!.SetValue(doctor, _doctorId);
        doctor.AddAvailability(_clinicId, DayOfWeek.Tuesday, new TimeOnly(9, 0), new TimeOnly(17, 0));
        _doctorRepository.Seed(doctor);

        var patient = Patient.Create(_tenantId, Guid.NewGuid(), new DateOnly(1995, 5, 20), Gender.Male);
        typeof(Patient).GetProperty(nameof(Patient.Id))!.SetValue(patient, _patientId);
        _patientRepository.Seed(patient);

        _dateTimeProvider.UtcNow = new DateTime(2026, 11, 2, 8, 0, 0, DateTimeKind.Utc); // Monday Nov 2
    }

    [Fact]
    public async Task BookAppointment_InArabic_ReturnsArabicStatusDescription()
    {
        // Arrange
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("ar-EG");
            var localizationService = new LocalizationService();

            var handler = new BookAppointmentCommandHandler(
                _appointmentRepository,
                _doctorRepository,
                _patientRepository,
                _tenantRepository,
                _dateTimeProvider,
                _unitOfWork,
                localizationService);

            // Tuesday Nov 3, 2026 10:00 - 10:30 UTC
            var command = new BookAppointmentCommand(
                _tenantId,
                _doctorId,
                _patientId,
                _clinicId,
                new DateTime(2026, 11, 3, 10, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 11, 3, 10, 30, 0, DateTimeKind.Utc),
                "فحص دوري");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("مجدول", result.Value.StatusDescription);
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Fact]
    public async Task BookAppointment_InPast_ReturnsArabicErrorMessage()
    {
        // Arrange
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("ar-EG");
            var localizationService = new LocalizationService();

            var handler = new BookAppointmentCommandHandler(
                _appointmentRepository,
                _doctorRepository,
                _patientRepository,
                _tenantRepository,
                _dateTimeProvider,
                _unitOfWork,
                localizationService);

            // Time in the past: 1 hour ago
            var pastTime = _dateTimeProvider.UtcNow.AddHours(-1);
            var command = new BookAppointmentCommand(
                _tenantId,
                _doctorId,
                _patientId,
                _clinicId,
                pastTime,
                pastTime.AddMinutes(30));

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("لا يمكن حجز موعد في تاريخ أو وقت سابق.", result.Error.Description);
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Fact]
    public async Task CancelAppointment_WithInsufficientNotice_ReturnsArabicPolicyMessage()
    {
        // Arrange
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("ar-EG");
            var localizationService = new LocalizationService();

            // Appointment starts in 2 hours (policy is 24 hours)
            var startUtc = _dateTimeProvider.UtcNow.AddHours(2);
            var appointment = Appointment.Create(
                _tenantId,
                _doctorId,
                _patientId,
                _clinicId,
                DateTimeRange.Create(startUtc, startUtc.AddMinutes(30)),
                _dateTimeProvider.UtcNow,
                requiresDoctorApproval: false);

            _appointmentRepository.Seed(appointment);

            var handler = new CancelAppointmentCommandHandler(
                _appointmentRepository,
                _tenantRepository,
                _dateTimeProvider,
                _unitOfWork,
                localizationService);

            var command = new CancelAppointmentCommand(
                appointment.Id,
                "ظرف طارئ",
                "Patient");

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Equal("Appointment.CancellationPolicyViolation", result.Error.Code);
            Assert.Equal("لا يمكن إلغاء الموعد لانتهاء المهلة المحددة مسبقاً لإلغاء المواعيد.", result.Error.Description);
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }
}
