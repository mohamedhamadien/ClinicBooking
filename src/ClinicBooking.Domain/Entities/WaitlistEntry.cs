using ClinicBooking.Domain.Common;
using ClinicBooking.Domain.Enums;
using ClinicBooking.Domain.Exceptions;

namespace ClinicBooking.Domain.Entities;

public sealed class WaitlistEntry : AggregateRoot<Guid>, IAuditableEntity, IMustHaveTenant
{
    public Guid TenantId { get; private set; }
    public Guid ClinicId { get; private set; }
    public Guid DoctorId { get; private set; }
    public Guid PatientId { get; private set; }
    public DateOnly PreferredDate { get; private set; }
    public TimeOnly? PreferredStartTime { get; private set; }
    public TimeOnly? PreferredEndTime { get; private set; }
    public int Priority { get; private set; } = 1;
    public string? Notes { get; private set; }
    public WaitlistStatus Status { get; private set; } = WaitlistStatus.Waiting;
    public DateTime? OfferedSlotStartUtc { get; private set; }
    public DateTime? OfferedSlotEndUtc { get; private set; }
    public DateTime? OfferExpiresAtUtc { get; private set; }
    public Guid? BookedAppointmentId { get; private set; }

    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModifiedAtUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    private WaitlistEntry()
    {
    }

    public static WaitlistEntry Create(
        Guid tenantId,
        Guid clinicId,
        Guid doctorId,
        Guid patientId,
        DateOnly preferredDate,
        TimeOnly? preferredStartTime = null,
        TimeOnly? preferredEndTime = null,
        int priority = 1,
        string? notes = null)
    {
        if (tenantId == Guid.Empty)
            throw new DomainValidationException("TenantId is required.");
        if (clinicId == Guid.Empty)
            throw new DomainValidationException("ClinicId is required.");
        if (doctorId == Guid.Empty)
            throw new DomainValidationException("DoctorId is required.");
        if (patientId == Guid.Empty)
            throw new DomainValidationException("PatientId is required.");
        if (preferredStartTime.HasValue && preferredEndTime.HasValue && preferredEndTime <= preferredStartTime)
            throw new DomainValidationException("PreferredEndTime must be greater than PreferredStartTime.");

        return new WaitlistEntry
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ClinicId = clinicId,
            DoctorId = doctorId,
            PatientId = patientId,
            PreferredDate = preferredDate,
            PreferredStartTime = preferredStartTime,
            PreferredEndTime = preferredEndTime,
            Priority = Math.Clamp(priority, 1, 10),
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            Status = WaitlistStatus.Waiting
        };
    }

    public void OfferSlot(DateTime slotStartUtc, DateTime slotEndUtc, DateTime expiresAtUtc)
    {
        if (Status != WaitlistStatus.Waiting)
            throw new DomainValidationException($"Cannot offer a slot to a waitlist entry in status '{Status}'.");
        if (slotEndUtc <= slotStartUtc)
            throw new DomainValidationException("Offered slot end time must be after start time.");

        Status = WaitlistStatus.Offered;
        OfferedSlotStartUtc = DateTime.SpecifyKind(slotStartUtc, DateTimeKind.Utc);
        OfferedSlotEndUtc = DateTime.SpecifyKind(slotEndUtc, DateTimeKind.Utc);
        OfferExpiresAtUtc = DateTime.SpecifyKind(expiresAtUtc, DateTimeKind.Utc);
    }

    public void MarkBooked(Guid appointmentId)
    {
        if (Status is not (WaitlistStatus.Waiting or WaitlistStatus.Offered))
            throw new DomainValidationException($"Cannot mark waitlist entry as booked from status '{Status}'.");
        if (appointmentId == Guid.Empty)
            throw new DomainValidationException("AppointmentId is required.");

        Status = WaitlistStatus.Booked;
        BookedAppointmentId = appointmentId;
    }

    public void ExpireOffer()
    {
        if (Status != WaitlistStatus.Offered)
            throw new DomainValidationException($"Only an offered waitlist entry can expire (current: '{Status}').");

        Status = WaitlistStatus.Expired;
    }

    public void Cancel()
    {
        if (Status is WaitlistStatus.Booked or WaitlistStatus.Cancelled)
            throw new DomainValidationException($"Cannot cancel waitlist entry in status '{Status}'.");

        Status = WaitlistStatus.Cancelled;
    }
}
