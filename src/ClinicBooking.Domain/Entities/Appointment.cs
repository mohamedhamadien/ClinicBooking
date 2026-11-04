using ClinicBooking.Domain.Common;
using ClinicBooking.Domain.Enums;
using ClinicBooking.Domain.Events;
using ClinicBooking.Domain.Exceptions;
using ClinicBooking.Domain.ValueObjects;

namespace ClinicBooking.Domain.Entities;

public sealed class Appointment : AggregateRoot<Guid>, IAuditableEntity
{
    public Guid DoctorId { get; private set; }
    public Guid PatientId { get; private set; }
    public Guid ClinicId { get; private set; }
    public DateTimeRange ScheduledRange { get; private set; } = default!;
    public AppointmentStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public string? CancellationReason { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModifiedAtUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    public bool IsSlotOccupied =>
        Status is not (AppointmentStatus.CancelledByPatient
                    or AppointmentStatus.CancelledByDoctor
                    or AppointmentStatus.CancelledByAdmin
                    or AppointmentStatus.Rejected);

    private Appointment() { }

    private Appointment(
        Guid id,
        Guid doctorId,
        Guid patientId,
        Guid clinicId,
        DateTimeRange scheduledRange,
        AppointmentStatus initialStatus,
        string? notes) : base(id)
    {
        DoctorId = doctorId;
        PatientId = patientId;
        ClinicId = clinicId;
        ScheduledRange = scheduledRange;
        Status = initialStatus;
        Notes = notes?.Trim();

        AddDomainEvent(new AppointmentBookedEvent(
            Id,
            DoctorId,
            PatientId,
            ClinicId,
            ScheduledRange.StartUtc,
            ScheduledRange.EndUtc));
    }

    public static Appointment Create(
        Guid doctorId,
        Guid patientId,
        Guid clinicId,
        DateTimeRange scheduledRange,
        DateTime currentUtc,
        bool requiresDoctorApproval = false,
        string? notes = null)
    {
        if (doctorId == Guid.Empty) throw new DomainValidationException("Doctor ID cannot be empty.");
        if (patientId == Guid.Empty) throw new DomainValidationException("Patient ID cannot be empty.");
        if (clinicId == Guid.Empty) throw new DomainValidationException("Clinic ID cannot be empty.");
        ArgumentNullException.ThrowIfNull(scheduledRange);

        var normalizedCurrentUtc = currentUtc.Kind == DateTimeKind.Utc ? currentUtc : currentUtc.ToUniversalTime();

        if (scheduledRange.StartUtc <= normalizedCurrentUtc)
        {
            throw new PastAppointmentException(scheduledRange.StartUtc, normalizedCurrentUtc);
        }

        var initialStatus = requiresDoctorApproval
            ? AppointmentStatus.PendingApproval
            : AppointmentStatus.Scheduled;

        return new Appointment(
            Guid.NewGuid(),
            doctorId,
            patientId,
            clinicId,
            scheduledRange,
            initialStatus,
            notes);
    }

    public void Confirm()
    {
        if (Status is not (AppointmentStatus.Scheduled or AppointmentStatus.PendingApproval))
        {
            throw new InvalidAppointmentStateException(Status, nameof(Confirm));
        }

        Status = AppointmentStatus.Confirmed;
        AddDomainEvent(new AppointmentConfirmedEvent(Id, DoctorId, PatientId));
    }

    public void Reject(string reason)
    {
        if (Status != AppointmentStatus.PendingApproval)
        {
            throw new InvalidAppointmentStateException(Status, nameof(Reject));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainValidationException("Rejection reason must be provided.");
        }

        Status = AppointmentStatus.Rejected;
        CancellationReason = reason.Trim();
        CancelledAtUtc = DateTime.UtcNow;

        AddDomainEvent(new AppointmentRejectedEvent(Id, DoctorId, PatientId, CancellationReason));
    }

    public void Complete()
    {
        if (Status != AppointmentStatus.Confirmed)
        {
            throw new InvalidAppointmentStateException(Status, nameof(Complete));
        }

        Status = AppointmentStatus.Completed;
        AddDomainEvent(new AppointmentCompletedEvent(Id, DoctorId, PatientId));
    }

    public void CancelByPatient(string reason, int minimumNoticeHours, DateTime currentUtc)
    {
        EnsureCancellable();

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainValidationException("Cancellation reason must be provided.");
        }

        var normalizedCurrentUtc = currentUtc.Kind == DateTimeKind.Utc ? currentUtc : currentUtc.ToUniversalTime();
        var noticeHours = (ScheduledRange.StartUtc - normalizedCurrentUtc).TotalHours;

        if (noticeHours < minimumNoticeHours)
        {
            throw new AppointmentCancellationPolicyException(minimumNoticeHours, noticeHours);
        }

        Status = AppointmentStatus.CancelledByPatient;
        CancellationReason = reason.Trim();
        CancelledAtUtc = normalizedCurrentUtc;

        AddDomainEvent(new AppointmentCancelledEvent(Id, DoctorId, PatientId, "Patient", CancellationReason));
    }

    public void CancelByDoctor(string reason, DateTime currentUtc)
    {
        EnsureCancellable();

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainValidationException("Cancellation reason must be provided.");
        }

        var normalizedCurrentUtc = currentUtc.Kind == DateTimeKind.Utc ? currentUtc : currentUtc.ToUniversalTime();

        Status = AppointmentStatus.CancelledByDoctor;
        CancellationReason = reason.Trim();
        CancelledAtUtc = normalizedCurrentUtc;

        AddDomainEvent(new AppointmentCancelledEvent(Id, DoctorId, PatientId, "Doctor", CancellationReason));
    }

    public void CancelByAdmin(string reason, DateTime currentUtc)
    {
        EnsureCancellable();

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainValidationException("Cancellation reason must be provided.");
        }

        var normalizedCurrentUtc = currentUtc.Kind == DateTimeKind.Utc ? currentUtc : currentUtc.ToUniversalTime();

        Status = AppointmentStatus.CancelledByAdmin;
        CancellationReason = reason.Trim();
        CancelledAtUtc = normalizedCurrentUtc;

        AddDomainEvent(new AppointmentCancelledEvent(Id, DoctorId, PatientId, "Admin", CancellationReason));
    }

    public void Reschedule(DateTimeRange newRange, DateTime currentUtc)
    {
        EnsureCancellable();
        ArgumentNullException.ThrowIfNull(newRange);

        var normalizedCurrentUtc = currentUtc.Kind == DateTimeKind.Utc ? currentUtc : currentUtc.ToUniversalTime();
        if (newRange.StartUtc <= normalizedCurrentUtc)
        {
            throw new PastAppointmentException(newRange.StartUtc, normalizedCurrentUtc);
        }

        var oldStart = ScheduledRange.StartUtc;
        ScheduledRange = newRange;

        AddDomainEvent(new AppointmentRescheduledEvent(
            Id,
            DoctorId,
            PatientId,
            oldStart,
            newRange.StartUtc,
            newRange.EndUtc));
    }

    public void MarkNoShow()
    {
        if (Status != AppointmentStatus.Confirmed)
        {
            throw new InvalidAppointmentStateException(Status, nameof(MarkNoShow));
        }

        Status = AppointmentStatus.NoShow;
    }

    private void EnsureCancellable()
    {
        if (Status is AppointmentStatus.Completed
            or AppointmentStatus.CancelledByPatient
            or AppointmentStatus.CancelledByDoctor
            or AppointmentStatus.CancelledByAdmin
            or AppointmentStatus.Rejected
            or AppointmentStatus.NoShow)
        {
            throw new InvalidAppointmentStateException(Status, "Cancel");
        }
    }
}
