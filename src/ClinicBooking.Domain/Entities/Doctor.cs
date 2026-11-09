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
    public string BioEn { get; private set; } = default!;
    public string BioAr { get; private set; } = default!;
    public int DefaultSlotDurationMinutes { get; private set; }
    public bool RequiresApproval { get; private set; }
    public bool IsActive { get; private set; }

    /// <summary>
    /// Default display bio (English).
    /// </summary>
    public string Bio => BioEn;

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
        string bioEn,
        int defaultSlotDurationMinutes = 30,
        bool requiresApproval = false,
        bool isActive = true,
        string? bioAr = null) : base(id)
    {
        if (tenantId == Guid.Empty) throw new DomainValidationException("Tenant ID cannot be empty.");
        TenantId = tenantId;
        UserId = userId;
        SpecialtyId = specialtyId;
        SetLicenseNumber(licenseNumber);
        SetBio(bioEn, bioAr);
        SetSlotDuration(defaultSlotDurationMinutes);
        RequiresApproval = requiresApproval;
        IsActive = isActive;
    }

    public static Doctor Create(
        Guid tenantId,
        Guid userId,
        Guid specialtyId,
        string licenseNumber,
        string bioEn,
        int defaultSlotDurationMinutes = 30,
        bool requiresApproval = false,
        string? bioAr = null)
    {
        return new Doctor(
            Guid.NewGuid(),
            tenantId,
            userId,
            specialtyId,
            licenseNumber,
            bioEn,
            defaultSlotDurationMinutes,
            requiresApproval,
            true,
            bioAr);
    }

    public void SetLicenseNumber(string licenseNumber)
    {
        if (string.IsNullOrWhiteSpace(licenseNumber))
        {
            throw new DomainValidationException("Doctor license number cannot be empty.");
        }

        LicenseNumber = licenseNumber.Trim().ToUpperInvariant();
    }

    public void SetBio(string bioEn, string? bioAr = null)
    {
        BioEn = bioEn?.Trim() ?? string.Empty;
        BioAr = string.IsNullOrWhiteSpace(bioAr) ? BioEn : bioAr.Trim();
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

    public void UpdateProfile(string bioEn, int defaultSlotDurationMinutes, bool requiresApproval, bool isActive, string? bioAr = null)
    {
        SetBio(bioEn, bioAr);
        SetSlotDuration(defaultSlotDurationMinutes);
        RequiresApproval = requiresApproval;
        IsActive = isActive;
    }

    public string GetLocalizedBio(bool isArabic) => isArabic && !string.IsNullOrWhiteSpace(BioAr) ? BioAr : BioEn;

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

    public void AssignToClinic(Guid clinicId, decimal consultationFee, bool isPrimary = false)
    {
        var existing = _doctorClinics.FirstOrDefault(dc => dc.ClinicId == clinicId);
        if (existing is not null)
        {
            existing.UpdateFee(consultationFee);
            existing.SetPrimary(isPrimary);
        }
        else
        {
            _doctorClinics.Add(new DoctorClinic(Guid.NewGuid(), TenantId, Id, clinicId, consultationFee, isPrimary));
        }
    }

    public void RemoveFromClinic(Guid clinicId)
    {
        var existing = _doctorClinics.FirstOrDefault(dc => dc.ClinicId == clinicId);
        if (existing is not null)
        {
            _doctorClinics.Remove(existing);
        }
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
