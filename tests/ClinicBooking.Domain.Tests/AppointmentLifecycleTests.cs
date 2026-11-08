using ClinicBooking.Domain.Entities;
using ClinicBooking.Domain.Enums;
using ClinicBooking.Domain.Events;
using ClinicBooking.Domain.Exceptions;
using ClinicBooking.Domain.ValueObjects;

namespace ClinicBooking.Domain.Tests;

public class AppointmentLifecycleTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _doctorId = Guid.NewGuid();
    private readonly Guid _patientId = Guid.NewGuid();
    private readonly Guid _clinicId = Guid.NewGuid();
    private readonly DateTime _currentUtc = new(2026, 11, 4, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_WithValidFutureDate_CreatesInScheduledStateAndEmitsEvent()
    {
        // Arrange
        var range = DateTimeRange.Create(_currentUtc.AddDays(1), _currentUtc.AddDays(1).AddMinutes(30));

        // Act
        var appointment = Appointment.Create(
            _tenantId,
            _doctorId,
            _patientId,
            _clinicId,
            range,
            _currentUtc,
            requiresDoctorApproval: false,
            notes: "Routine checkup");

        // Assert
        Assert.NotEqual(Guid.Empty, appointment.Id);
        Assert.Equal(_tenantId, appointment.TenantId);
        Assert.Equal(_doctorId, appointment.DoctorId);
        Assert.Equal(_patientId, appointment.PatientId);
        Assert.Equal(_clinicId, appointment.ClinicId);
        Assert.Equal(AppointmentStatus.Scheduled, appointment.Status);
        Assert.True(appointment.IsSlotOccupied);
        Assert.Equal("Routine checkup", appointment.Notes);
        Assert.Contains(appointment.DomainEvents, e => e is AppointmentBookedEvent);
    }

    [Fact]
    public void Create_WhenApprovalRequired_CreatesInPendingApprovalState()
    {
        // Arrange
        var range = DateTimeRange.Create(_currentUtc.AddDays(1), _currentUtc.AddDays(1).AddMinutes(30));

        // Act
        var appointment = Appointment.Create(
            _tenantId,
            _doctorId,
            _patientId,
            _clinicId,
            range,
            _currentUtc,
            requiresDoctorApproval: true);

        // Assert
        Assert.Equal(AppointmentStatus.PendingApproval, appointment.Status);
        Assert.True(appointment.IsSlotOccupied);
    }

    [Fact]
    public void Create_WithPastDate_ThrowsPastAppointmentException()
    {
        // Arrange
        var pastRange = DateTimeRange.Create(_currentUtc.AddHours(-2), _currentUtc.AddHours(-1));

        // Act & Assert
        Assert.Throws<PastAppointmentException>(() => Appointment.Create(
            _tenantId,
            _doctorId,
            _patientId,
            _clinicId,
            pastRange,
            _currentUtc));
    }

    [Fact]
    public void Create_WithEmptyTenantId_ThrowsDomainValidationException()
    {
        // Arrange
        var range = DateTimeRange.Create(_currentUtc.AddDays(1), _currentUtc.AddDays(1).AddMinutes(30));

        // Act & Assert
        Assert.Throws<DomainValidationException>(() => Appointment.Create(
            Guid.Empty,
            _doctorId,
            _patientId,
            _clinicId,
            range,
            _currentUtc));
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000", "e7d17e7d-17e7-17e7-17e7-17e7d17e7d17", "f8e28f8e-28f8-28f8-28f8-28f8e28f8e28")]
    [InlineData("e7d17e7d-17e7-17e7-17e7-17e7d17e7d17", "00000000-0000-0000-0000-000000000000", "f8e28f8e-28f8-28f8-28f8-28f8e28f8e28")]
    [InlineData("e7d17e7d-17e7-17e7-17e7-17e7d17e7d17", "f8e28f8e-28f8-28f8-28f8-28f8e28f8e28", "00000000-0000-0000-0000-000000000000")]
    public void Create_WithEmptyEntityIds_ThrowsDomainValidationException(string docId, string patId, string clinId)
    {
        // Arrange
        var range = DateTimeRange.Create(_currentUtc.AddDays(1), _currentUtc.AddDays(1).AddMinutes(30));

        // Act & Assert
        Assert.Throws<DomainValidationException>(() => Appointment.Create(
            _tenantId,
            Guid.Parse(docId),
            Guid.Parse(patId),
            Guid.Parse(clinId),
            range,
            _currentUtc));
    }

    [Fact]
    public void Confirm_FromPendingApproval_TransitionsToConfirmed()
    {
        // Arrange
        var range = DateTimeRange.Create(_currentUtc.AddDays(1), _currentUtc.AddDays(1).AddMinutes(30));
        var appointment = Appointment.Create(_tenantId, _doctorId, _patientId, _clinicId, range, _currentUtc, requiresDoctorApproval: true);

        // Act
        appointment.Confirm();

        // Assert
        Assert.Equal(AppointmentStatus.Confirmed, appointment.Status);
        Assert.True(appointment.IsSlotOccupied);
        Assert.Contains(appointment.DomainEvents, e => e is AppointmentConfirmedEvent);
    }

    [Fact]
    public void Reject_FromPendingApproval_TransitionsToRejectedAndReleasesSlot()
    {
        // Arrange
        var range = DateTimeRange.Create(_currentUtc.AddDays(1), _currentUtc.AddDays(1).AddMinutes(30));
        var appointment = Appointment.Create(_tenantId, _doctorId, _patientId, _clinicId, range, _currentUtc, requiresDoctorApproval: true);

        // Act
        appointment.Reject("Doctor is called for emergency surgery");

        // Assert
        Assert.Equal(AppointmentStatus.Rejected, appointment.Status);
        Assert.False(appointment.IsSlotOccupied);
        Assert.Equal("Doctor is called for emergency surgery", appointment.CancellationReason);
        Assert.NotNull(appointment.CancelledAtUtc);
        Assert.Contains(appointment.DomainEvents, e => e is AppointmentRejectedEvent);
    }

    [Fact]
    public void Reject_FromScheduled_ThrowsInvalidAppointmentStateException()
    {
        // Arrange
        var range = DateTimeRange.Create(_currentUtc.AddDays(1), _currentUtc.AddDays(1).AddMinutes(30));
        var appointment = Appointment.Create(_tenantId, _doctorId, _patientId, _clinicId, range, _currentUtc, requiresDoctorApproval: false);

        // Act & Assert
        Assert.Throws<InvalidAppointmentStateException>(() => appointment.Reject("Not pending"));
    }

    [Fact]
    public void Complete_FromConfirmed_TransitionsToCompleted()
    {
        // Arrange
        var range = DateTimeRange.Create(_currentUtc.AddDays(1), _currentUtc.AddDays(1).AddMinutes(30));
        var appointment = Appointment.Create(_tenantId, _doctorId, _patientId, _clinicId, range, _currentUtc, requiresDoctorApproval: false);
        appointment.Confirm();

        // Act
        appointment.Complete();

        // Assert
        Assert.Equal(AppointmentStatus.Completed, appointment.Status);
        Assert.Contains(appointment.DomainEvents, e => e is AppointmentCompletedEvent);
    }

    [Fact]
    public void Complete_FromScheduledWithoutConfirmation_ThrowsInvalidAppointmentStateException()
    {
        // Arrange
        var range = DateTimeRange.Create(_currentUtc.AddDays(1), _currentUtc.AddDays(1).AddMinutes(30));
        var appointment = Appointment.Create(_tenantId, _doctorId, _patientId, _clinicId, range, _currentUtc, requiresDoctorApproval: false);

        // Act & Assert
        Assert.Throws<InvalidAppointmentStateException>(() => appointment.Complete());
    }

    [Fact]
    public void CancelByPatient_WithSufficientNotice_TransitionsToCancelledByPatientAndReleasesSlot()
    {
        // Arrange: appointment is in 48 hours, notice requirement is 24 hours
        var range = DateTimeRange.Create(_currentUtc.AddHours(48), _currentUtc.AddHours(48).AddMinutes(30));
        var appointment = Appointment.Create(_tenantId, _doctorId, _patientId, _clinicId, range, _currentUtc);

        // Act
        appointment.CancelByPatient("Schedule conflict", minimumNoticeHours: 24, currentUtc: _currentUtc);

        // Assert
        Assert.Equal(AppointmentStatus.CancelledByPatient, appointment.Status);
        Assert.False(appointment.IsSlotOccupied);
        Assert.Equal("Schedule conflict", appointment.CancellationReason);
        Assert.Equal(_currentUtc, appointment.CancelledAtUtc);
        Assert.Contains(appointment.DomainEvents, e => e is AppointmentCancelledEvent);
    }

    [Fact]
    public void CancelByPatient_WithInsufficientNotice_ThrowsAppointmentCancellationPolicyException()
    {
        // Arrange: appointment is in 10 hours, notice requirement is 24 hours
        var range = DateTimeRange.Create(_currentUtc.AddHours(10), _currentUtc.AddHours(10).AddMinutes(30));
        var appointment = Appointment.Create(_tenantId, _doctorId, _patientId, _clinicId, range, _currentUtc);

        // Act & Assert
        Assert.Throws<AppointmentCancellationPolicyException>(() =>
            appointment.CancelByPatient("Last minute change", minimumNoticeHours: 24, currentUtc: _currentUtc));
    }

    [Fact]
    public void CancelByDoctor_TransitionsToCancelledByDoctorAndReleasesSlot()
    {
        // Arrange
        var range = DateTimeRange.Create(_currentUtc.AddHours(2), _currentUtc.AddHours(2).AddMinutes(30));
        var appointment = Appointment.Create(_tenantId, _doctorId, _patientId, _clinicId, range, _currentUtc);

        // Act
        appointment.CancelByDoctor("Doctor sick leave", _currentUtc);

        // Assert
        Assert.Equal(AppointmentStatus.CancelledByDoctor, appointment.Status);
        Assert.False(appointment.IsSlotOccupied);
        Assert.Equal("Doctor sick leave", appointment.CancellationReason);
    }

    [Fact]
    public void CancelByAdmin_TransitionsToCancelledByAdminAndReleasesSlot()
    {
        // Arrange
        var range = DateTimeRange.Create(_currentUtc.AddHours(2), _currentUtc.AddHours(2).AddMinutes(30));
        var appointment = Appointment.Create(_tenantId, _doctorId, _patientId, _clinicId, range, _currentUtc);

        // Act
        appointment.CancelByAdmin("Clinic maintenance", _currentUtc);

        // Assert
        Assert.Equal(AppointmentStatus.CancelledByAdmin, appointment.Status);
        Assert.False(appointment.IsSlotOccupied);
        Assert.Equal("Clinic maintenance", appointment.CancellationReason);
    }

    [Fact]
    public void Reschedule_WithValidFutureRange_UpdatesScheduledRange()
    {
        // Arrange
        var initialRange = DateTimeRange.Create(_currentUtc.AddDays(1), _currentUtc.AddDays(1).AddMinutes(30));
        var newRange = DateTimeRange.Create(_currentUtc.AddDays(3), _currentUtc.AddDays(3).AddMinutes(30));
        var appointment = Appointment.Create(_tenantId, _doctorId, _patientId, _clinicId, initialRange, _currentUtc);

        // Act
        appointment.Reschedule(newRange, _currentUtc);

        // Assert
        Assert.Equal(newRange, appointment.ScheduledRange);
        Assert.Contains(appointment.DomainEvents, e => e is AppointmentRescheduledEvent);
    }

    [Fact]
    public void Reschedule_WithPastRange_ThrowsPastAppointmentException()
    {
        // Arrange
        var initialRange = DateTimeRange.Create(_currentUtc.AddDays(1), _currentUtc.AddDays(1).AddMinutes(30));
        var pastRange = DateTimeRange.Create(_currentUtc.AddHours(-1), _currentUtc.AddMinutes(-30));
        var appointment = Appointment.Create(_tenantId, _doctorId, _patientId, _clinicId, initialRange, _currentUtc);

        // Act & Assert
        Assert.Throws<PastAppointmentException>(() => appointment.Reschedule(pastRange, _currentUtc));
    }

    [Fact]
    public void MarkNoShow_FromConfirmed_TransitionsToNoShow()
    {
        // Arrange
        var range = DateTimeRange.Create(_currentUtc.AddDays(1), _currentUtc.AddDays(1).AddMinutes(30));
        var appointment = Appointment.Create(_tenantId, _doctorId, _patientId, _clinicId, range, _currentUtc);
        appointment.Confirm();

        // Act
        appointment.MarkNoShow();

        // Assert
        Assert.Equal(AppointmentStatus.NoShow, appointment.Status);
    }

    [Fact]
    public void MarkNoShow_FromScheduled_ThrowsInvalidAppointmentStateException()
    {
        // Arrange
        var range = DateTimeRange.Create(_currentUtc.AddDays(1), _currentUtc.AddDays(1).AddMinutes(30));
        var appointment = Appointment.Create(_tenantId, _doctorId, _patientId, _clinicId, range, _currentUtc);

        // Act & Assert
        Assert.Throws<InvalidAppointmentStateException>(() => appointment.MarkNoShow());
    }
}
