using ClinicBooking.Application.Features.Appointments.Commands.CancelAppointment;
using ClinicBooking.Application.Tests.TestHelpers;
using ClinicBooking.Domain.Entities;
using ClinicBooking.Domain.Enums;
using ClinicBooking.Domain.ValueObjects;

namespace ClinicBooking.Application.Tests.Features.Appointments;

public class CancelAppointmentCommandHandlerTests
{
    private readonly FakeAppointmentRepository _appointmentRepository = new();
    private readonly FakeTenantRepository _tenantRepository = new();
    private readonly FakeDateTimeProvider _dateTimeProvider = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _doctorId = Guid.NewGuid();
    private readonly Guid _patientId = Guid.NewGuid();
    private readonly Guid _clinicId = Guid.NewGuid();

    public CancelAppointmentCommandHandlerTests()
    {
        // Tenant with 24 hours notice required
        var tenant = Tenant.Create("Standard Clinic", "std-clinic", cancellationNoticeHours: 24);
        typeof(Tenant).GetProperty(nameof(Tenant.Id))!.SetValue(tenant, _tenantId);
        _tenantRepository.Seed(tenant);

        // System clock: Nov 1, 2026 10:00 UTC
        _dateTimeProvider.UtcNow = new DateTime(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc);
    }

    [Fact]
    public async Task Handle_PatientCancellationWithSufficientNotice_SucceedsAndReleasesSlot()
    {
        // Arrange: Appointment in 48 hours (Nov 3, 10:00 UTC)
        var startUtc = _dateTimeProvider.UtcNow.AddHours(48);
        var endUtc = startUtc.AddMinutes(30);

        var appointment = Appointment.Create(
            _tenantId,
            _doctorId,
            _patientId,
            _clinicId,
            DateTimeRange.Create(startUtc, endUtc),
            _dateTimeProvider.UtcNow);

        _appointmentRepository.Seed(appointment);

        var command = new CancelAppointmentCommand(appointment.Id, "Schedule conflict", "Patient");
        var handler = new CancelAppointmentCommandHandler(
            _appointmentRepository,
            _tenantRepository,
            _dateTimeProvider,
            _unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(AppointmentStatus.CancelledByPatient, result.Value.Status);
        Assert.False(appointment.IsSlotOccupied);
        Assert.Equal("Schedule conflict", result.Value.CancellationReason);
        Assert.Equal(1, _unitOfWork.SaveChangesCallsCount);
    }

    [Fact]
    public async Task Handle_PatientCancellationWithInsufficientNotice_ReturnsCancellationPolicyViolation()
    {
        // Arrange: Appointment in 10 hours (Notice required is 24 hours)
        var startUtc = _dateTimeProvider.UtcNow.AddHours(10);
        var endUtc = startUtc.AddMinutes(30);

        var appointment = Appointment.Create(
            _tenantId,
            _doctorId,
            _patientId,
            _clinicId,
            DateTimeRange.Create(startUtc, endUtc),
            _dateTimeProvider.UtcNow);

        _appointmentRepository.Seed(appointment);

        var command = new CancelAppointmentCommand(appointment.Id, "Urgent change", "Patient");
        var handler = new CancelAppointmentCommandHandler(
            _appointmentRepository,
            _tenantRepository,
            _dateTimeProvider,
            _unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Appointment.CancellationPolicyViolation", result.Error.Code);
        Assert.True(appointment.IsSlotOccupied); // slot is NOT released
        Assert.Equal(0, _unitOfWork.SaveChangesCallsCount);
    }

    [Fact]
    public async Task Handle_DoctorCancellation_SucceedsWithoutNoticeLeadTime()
    {
        // Arrange: Appointment in 2 hours
        var startUtc = _dateTimeProvider.UtcNow.AddHours(2);
        var endUtc = startUtc.AddMinutes(30);

        var appointment = Appointment.Create(
            _tenantId,
            _doctorId,
            _patientId,
            _clinicId,
            DateTimeRange.Create(startUtc, endUtc),
            _dateTimeProvider.UtcNow);

        _appointmentRepository.Seed(appointment);

        var command = new CancelAppointmentCommand(appointment.Id, "Emergency operation", "Doctor");
        var handler = new CancelAppointmentCommandHandler(
            _appointmentRepository,
            _tenantRepository,
            _dateTimeProvider,
            _unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(AppointmentStatus.CancelledByDoctor, result.Value.Status);
        Assert.False(appointment.IsSlotOccupied);
        Assert.Equal(1, _unitOfWork.SaveChangesCallsCount);
    }
}
