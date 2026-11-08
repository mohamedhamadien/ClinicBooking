using ClinicBooking.Domain.Common;
using ClinicBooking.Domain.ValueObjects;

namespace ClinicBooking.Domain.Entities;

public sealed class DoctorAvailability : Entity<Guid>, IAuditableEntity, IMustHaveTenant
{
    public Guid TenantId { get; private set; }
    public Guid DoctorId { get; private set; }
    public Guid ClinicId { get; private set; }
    public DayOfWeek DayOfWeek { get; private set; }
    public TimeSlot Shift { get; private set; } = default!;
    public bool IsActive { get; private set; }

    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModifiedAtUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    private DoctorAvailability() { }

    public DoctorAvailability(
        Guid id,
        Guid tenantId,
        Guid doctorId,
        Guid clinicId,
        DayOfWeek dayOfWeek,
        TimeSlot shift,
        bool isActive = true) : base(id)
    {
        TenantId = tenantId;
        DoctorId = doctorId;
        ClinicId = clinicId;
        DayOfWeek = dayOfWeek;
        Shift = shift ?? throw new ArgumentNullException(nameof(shift));
        IsActive = isActive;
    }

    public static DoctorAvailability Create(
        Guid tenantId,
        Guid doctorId,
        Guid clinicId,
        DayOfWeek dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime)
    {
        return new DoctorAvailability(
            Guid.NewGuid(),
            tenantId,
            doctorId,
            clinicId,
            dayOfWeek,
            TimeSlot.Create(startTime, endTime),
            true);
    }

    public void UpdateShift(TimeSlot shift)
    {
        Shift = shift ?? throw new ArgumentNullException(nameof(shift));
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
