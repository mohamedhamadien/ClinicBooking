using ClinicBooking.Application.Features.Appointments.Commands.BookAppointment;
using ClinicBooking.Application.Tests.TestHelpers;
using ClinicBooking.Domain.Entities;
using ClinicBooking.Domain.Enums;
using ClinicBooking.Domain.ValueObjects;

namespace ClinicBooking.Application.Tests.Features.Appointments;

public class BookAppointmentCommandHandlerTests
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

    public BookAppointmentCommandHandlerTests()
    {
        // Seed active tenant
        var tenant = Tenant.Create("Metropolitan Clinic", "metro-clinic");
        typeof(Tenant).GetProperty(nameof(Tenant.Id))!.SetValue(tenant, _tenantId);
        _tenantRepository.Seed(tenant);

        // Seed doctor with Monday shift 09:00 - 13:00
        var doctor = Doctor.Create(_tenantId, Guid.NewGuid(), Guid.NewGuid(), "LIC-999", "Pediatrician");
        typeof(Doctor).GetProperty(nameof(Doctor.Id))!.SetValue(doctor, _doctorId);
        doctor.AddAvailability(_clinicId, DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(13, 0));
        _doctorRepository.Seed(doctor);

        // Seed patient
        var patient = Patient.Create(_tenantId, Guid.NewGuid(), new DateOnly(1990, 1, 1), Gender.Female);
        typeof(Patient).GetProperty(nameof(Patient.Id))!.SetValue(patient, _patientId);
        _patientRepository.Seed(patient);

        // Set system clock: Sunday, Nov 1, 2026 12:00 UTC
        _dateTimeProvider.UtcNow = new DateTime(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc);
    }

    [Fact]
    public async Task Handle_WithValidSlotAndAvailability_SucceedsAndPersists()
    {
        // Arrange: Booking for Monday Nov 2, 2026 10:00 - 10:30 UTC
        var startUtc = new DateTime(2026, 11, 2, 10, 0, 0, DateTimeKind.Utc);
        var endUtc = new DateTime(2026, 11, 2, 10, 30, 0, DateTimeKind.Utc);

        var command = new BookAppointmentCommand(
            _tenantId,
            _doctorId,
            _patientId,
            _clinicId,
            startUtc,
            endUtc,
            "Consultation");

        var handler = new BookAppointmentCommandHandler(
            _appointmentRepository,
            _doctorRepository,
            _patientRepository,
            _tenantRepository,
            _dateTimeProvider,
            _unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(_tenantId, result.Value.TenantId);
        Assert.Equal(_doctorId, result.Value.DoctorId);
        Assert.Equal(_patientId, result.Value.PatientId);
        Assert.Equal(AppointmentStatus.Scheduled, result.Value.Status);
        Assert.Equal(1, _unitOfWork.SaveChangesCallsCount);
    }

    [Fact]
    public async Task Handle_WithPastSlot_ReturnsPastTimeError()
    {
        // Arrange: Start is earlier than current UtcNow
        var pastStart = _dateTimeProvider.UtcNow.AddHours(-1);
        var pastEnd = _dateTimeProvider.UtcNow.AddMinutes(-30);

        var command = new BookAppointmentCommand(
            _tenantId,
            _doctorId,
            _patientId,
            _clinicId,
            pastStart,
            pastEnd);

        var handler = new BookAppointmentCommandHandler(
            _appointmentRepository,
            _doctorRepository,
            _patientRepository,
            _tenantRepository,
            _dateTimeProvider,
            _unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Appointment.PastTime", result.Error.Code);
        Assert.Equal(0, _unitOfWork.SaveChangesCallsCount);
    }

    [Fact]
    public async Task Handle_WhenSlotIsOutsideWorkingHours_ReturnsOutsideWorkingHoursError()
    {
        // Arrange: Shift is 9:00 - 13:00 on Monday. Requesting 15:00 - 15:30
        var afternoonStart = new DateTime(2026, 11, 2, 15, 0, 0, DateTimeKind.Utc);
        var afternoonEnd = new DateTime(2026, 11, 2, 15, 30, 0, DateTimeKind.Utc);

        var command = new BookAppointmentCommand(
            _tenantId,
            _doctorId,
            _patientId,
            _clinicId,
            afternoonStart,
            afternoonEnd);

        var handler = new BookAppointmentCommandHandler(
            _appointmentRepository,
            _doctorRepository,
            _patientRepository,
            _tenantRepository,
            _dateTimeProvider,
            _unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Doctor.OutsideWorkingHours", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenDoctorHasBlackoutException_ReturnsDoctorBlackoutConflict()
    {
        // Arrange: Doctor takes emergency leave from 10:00 to 12:00 on Nov 2
        var doctor = await _doctorRepository.GetByIdWithSchedulesAsync(_doctorId);
        var leaveStart = new DateTime(2026, 11, 2, 10, 0, 0, DateTimeKind.Utc);
        var leaveEnd = new DateTime(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);
        doctor!.AddBlackoutException(leaveStart, leaveEnd, "Emergency Leave");

        var slotStart = new DateTime(2026, 11, 2, 10, 30, 0, DateTimeKind.Utc);
        var slotEnd = new DateTime(2026, 11, 2, 11, 0, 0, DateTimeKind.Utc);

        var command = new BookAppointmentCommand(
            _tenantId,
            _doctorId,
            _patientId,
            _clinicId,
            slotStart,
            slotEnd);

        var handler = new BookAppointmentCommandHandler(
            _appointmentRepository,
            _doctorRepository,
            _patientRepository,
            _tenantRepository,
            _dateTimeProvider,
            _unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Doctor.BlackoutException", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenDoctorAlreadyHasAppointmentInSlot_ReturnsDoctorSlotUnavailableConflict()
    {
        // Arrange: Existing active appointment for doctor at 10:00 - 10:30
        var slotStart = new DateTime(2026, 11, 2, 10, 0, 0, DateTimeKind.Utc);
        var slotEnd = new DateTime(2026, 11, 2, 10, 30, 0, DateTimeKind.Utc);

        var existingAppointment = Appointment.Create(
            _tenantId,
            _doctorId,
            Guid.NewGuid(), // another patient
            _clinicId,
            DateTimeRange.Create(slotStart, slotEnd),
            _dateTimeProvider.UtcNow);

        _appointmentRepository.Seed(existingAppointment);

        var command = new BookAppointmentCommand(
            _tenantId,
            _doctorId,
            _patientId,
            _clinicId,
            slotStart,
            slotEnd);

        var handler = new BookAppointmentCommandHandler(
            _appointmentRepository,
            _doctorRepository,
            _patientRepository,
            _tenantRepository,
            _dateTimeProvider,
            _unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Appointment.DoctorSlotUnavailable", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenPatientAlreadyHasAppointmentInSlot_ReturnsPatientDoubleBookingConflict()
    {
        // Arrange: Existing active appointment for THIS patient with another doctor
        var slotStart = new DateTime(2026, 11, 2, 10, 0, 0, DateTimeKind.Utc);
        var slotEnd = new DateTime(2026, 11, 2, 10, 30, 0, DateTimeKind.Utc);

        var existingAppointment = Appointment.Create(
            _tenantId,
            Guid.NewGuid(), // different doctor
            _patientId, // same patient
            _clinicId,
            DateTimeRange.Create(slotStart, slotEnd),
            _dateTimeProvider.UtcNow);

        _appointmentRepository.Seed(existingAppointment);

        var command = new BookAppointmentCommand(
            _tenantId,
            _doctorId,
            _patientId,
            _clinicId,
            slotStart,
            slotEnd);

        var handler = new BookAppointmentCommandHandler(
            _appointmentRepository,
            _doctorRepository,
            _patientRepository,
            _tenantRepository,
            _dateTimeProvider,
            _unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Appointment.PatientDoubleBooking", result.Error.Code);
    }
}
