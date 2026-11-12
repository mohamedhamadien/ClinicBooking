using ClinicBooking.Domain.Common;
using ClinicBooking.Domain.Enums;
using ClinicBooking.Domain.Events;
using ClinicBooking.Domain.Exceptions;
using ClinicBooking.Domain.ValueObjects;

namespace ClinicBooking.Domain.Entities;

public sealed class Appointment : AggregateRoot<Guid>, IAuditableEntity, IMustHaveTenant
{
    public Guid TenantId { get; private set; }
    public Guid DoctorId { get; private set; }
    public Guid PatientId { get; private set; }
    public Guid ClinicId { get; private set; }
    /// <summary>
    /// Persisted UTC start of the appointment. Stored as a first-class column so the database
    /// can enforce the filtered unique double-booking indexes on it.
    /// </summary>
    public DateTime StartTimeUtc { get; private set; }

    /// <summary>
    /// Persisted UTC end of the appointment.
    /// </summary>
    public DateTime EndTimeUtc { get; private set; }

    /// <summary>
    /// Domain view of the appointment time window, backed by <see cref="StartTimeUtc"/> and <see cref="EndTimeUtc"/>.
    /// </summary>
    public DateTimeRange ScheduledRange
    {
        get => DateTimeRange.Create(
            DateTime.SpecifyKind(StartTimeUtc, DateTimeKind.Utc),
            DateTime.SpecifyKind(EndTimeUtc, DateTimeKind.Utc));
        private set
        {
            ArgumentNullException.ThrowIfNull(value);
            StartTimeUtc = value.StartUtc;
            EndTimeUtc = value.EndUtc;
        }
    }
    public AppointmentStatus Status { get; private set; }
    public AppointmentSource Source { get; private set; } = AppointmentSource.Website;
    public Guid? CreatedByStaffId { get; private set; }
    public string? UtmSource { get; private set; }
    public string? UtmMedium { get; private set; }
    public string? UtmCampaign { get; private set; }
    public string? ExternalReference { get; private set; }
    public int? QueueNumber { get; private set; }
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
        Guid tenantId,
        Guid doctorId,
        Guid patientId,
        Guid clinicId,
        DateTimeRange scheduledRange,
        AppointmentStatus initialStatus,
        string? notes) : base(id)
    {
        TenantId = tenantId;
        DoctorId = doctorId;
        PatientId = patientId;
        ClinicId = clinicId;
        ScheduledRange = scheduledRange;
        Status = initialStatus;
        Notes = notes?.Trim();

        AddDomainEvent(new AppointmentBookedEvent(
            Id,
            TenantId,
            DoctorId,
            PatientId,
            ClinicId,
            ScheduledRange.StartUtc,
            ScheduledRange.EndUtc));
    }

    public static Appointment Create(
        Guid tenantId,
        Guid doctorId,
        Guid patientId,
        Guid clinicId,
        DateTimeRange scheduledRange,
        DateTime currentUtc,
        bool requiresDoctorApproval = false,
        string? notes = null)
    {
        if (tenantId == Guid.Empty) throw new DomainValidationException("Tenant ID cannot be empty.");
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
            tenantId,
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

    public void CheckIn(int queueNumber)
    {
        if (Status != AppointmentStatus.Scheduled && Status != AppointmentStatus.Confirmed)
        {
            throw new InvalidAppointmentStateException(Status, nameof(CheckIn));
        }

        Status = AppointmentStatus.CheckedIn;
        QueueNumber = queueNumber;
    }

    public void StartConsultation()
    {
        if (Status != AppointmentStatus.CheckedIn && Status != AppointmentStatus.Confirmed)
        {
            throw new InvalidAppointmentStateException(Status, nameof(StartConsultation));
        }

        Status = AppointmentStatus.InConsultation;
    }

    public void Complete()
    {
        if (Status != AppointmentStatus.Confirmed && Status != AppointmentStatus.CheckedIn && Status != AppointmentStatus.InConsultation)
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

    public void SetBookingMetadata(
        AppointmentSource source,
        Guid? createdByStaffId = null,
        string? utmSource = null,
        string? utmMedium = null,
        string? utmCampaign = null,
        string? externalReference = null)
    {
        Source = Enum.IsDefined(source) ? source : AppointmentSource.Website;
        CreatedByStaffId = createdByStaffId == Guid.Empty ? null : createdByStaffId;
        UtmSource = string.IsNullOrWhiteSpace(utmSource) ? null : utmSource.Trim();
        UtmMedium = string.IsNullOrWhiteSpace(utmMedium) ? null : utmMedium.Trim();
        UtmCampaign = string.IsNullOrWhiteSpace(utmCampaign) ? null : utmCampaign.Trim();
        ExternalReference = string.IsNullOrWhiteSpace(externalReference) ? null : externalReference.Trim();
    }

    public void MarkNoShow()
    {
        if (Status != AppointmentStatus.Confirmed && Status != AppointmentStatus.CheckedIn)
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
