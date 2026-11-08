using ClinicBooking.Domain.Common;
using ClinicBooking.Domain.Exceptions;
using ClinicBooking.Domain.ValueObjects;

namespace ClinicBooking.Domain.Entities;

public sealed class Doctor : AggregateRoot<Guid>, IAuditableEntity, IMustHaveTenant
{
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid SpecialtyId { get; private set; }
    public string LicenseNumber { get; private set; } = default!;
    public string Bio { get; private set; } = default!;
    public int DefaultSlotDurationMinutes { get; private set; }
    public bool RequiresApproval { get; private set; }
    public bool IsActive { get; private set; }

    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModifiedAtUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    private readonly List<DoctorAvailability> _availabilities = [];
    public IReadOnlyCollection<DoctorAvailability> Availabilities => _availabilities.AsReadOnly();

    private readonly List<AvailabilityException> _exceptions = [];
    public IReadOnlyCollection<AvailabilityException> Exceptions => _exceptions.AsReadOnly();

    private readonly List<DoctorClinic> _doctorClinics = [];
    public IReadOnlyCollection<DoctorClinic> DoctorClinics => _doctorClinics.AsReadOnly();

    private Doctor() { }

    public Doctor(
        Guid id,
        Guid tenantId,
        Guid userId,
        Guid specialtyId,
        string licenseNumber,
        string bio,
        int defaultSlotDurationMinutes = 30,
        bool requiresApproval = false,
        bool isActive = true) : base(id)
    {
        if (tenantId == Guid.Empty) throw new DomainValidationException("Tenant ID cannot be empty.");
        TenantId = tenantId;
        UserId = userId;
        SpecialtyId = specialtyId;
        SetLicenseNumber(licenseNumber);
        Bio = bio?.Trim() ?? string.Empty;
        SetSlotDuration(defaultSlotDurationMinutes);
        RequiresApproval = requiresApproval;
        IsActive = isActive;
    }

    public static Doctor Create(
        Guid tenantId,
        Guid userId,
        Guid specialtyId,
        string licenseNumber,
        string bio,
        int defaultSlotDurationMinutes = 30,
        bool requiresApproval = false)
    {
        return new Doctor(
            Guid.NewGuid(),
            tenantId,
            userId,
            specialtyId,
            licenseNumber,
            bio,
            defaultSlotDurationMinutes,
            requiresApproval,
            true);
    }

    public void SetLicenseNumber(string licenseNumber)
    {
        if (string.IsNullOrWhiteSpace(licenseNumber))
        {
            throw new DomainValidationException("Doctor license number cannot be empty.");
        }

        LicenseNumber = licenseNumber.Trim().ToUpperInvariant();
    }

    public void SetSlotDuration(int minutes)
    {
        if (minutes < 10 || minutes > 180)
        {
            throw new SlotDurationInvalidException(minutes);
        }

        DefaultSlotDurationMinutes = minutes;
    }

    public void SetApprovalRequirement(bool requiresApproval)
    {
        RequiresApproval = requiresApproval;
    }

    public void UpdateProfile(string bio, int defaultSlotDurationMinutes, bool requiresApproval, bool isActive)
    {
        Bio = bio?.Trim() ?? string.Empty;
        SetSlotDuration(defaultSlotDurationMinutes);
        RequiresApproval = requiresApproval;
        IsActive = isActive;
    }

    public void AddAvailability(Guid clinicId, DayOfWeek dayOfWeek, TimeOnly startTime, TimeOnly endTime)
    {
        var newShift = TimeSlot.Create(startTime, endTime);

        var hasOverlap = _availabilities
            .Where(a => a.IsActive && a.DayOfWeek == dayOfWeek)
            .Any(a => a.Shift.OverlapsWith(newShift));

        if (hasOverlap)
        {
            throw new DomainValidationException($"Doctor already has an active overlapping working shift on {dayOfWeek} between {startTime} and {endTime}.");
        }

        _availabilities.Add(new DoctorAvailability(
            Guid.NewGuid(),
            TenantId,
            Id,
            clinicId,
            dayOfWeek,
            newShift,
            true));
    }

    public void AddBlackoutException(DateTime startUtc, DateTime endUtc, string reason)
    {
        var exception = AvailabilityException.CreateBlock(TenantId, Id, startUtc, endUtc, reason);
        _exceptions.Add(exception);
    }

    public bool IsBlockedAt(DateTimeRange range)
    {
        return _exceptions.Any(e => e.IsBlocked && e.Range.OverlapsWith(range));
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
