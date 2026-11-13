using ClinicBooking.Domain.Common;
using ClinicBooking.Domain.Exceptions;
using ClinicBooking.Domain.ValueObjects;

namespace ClinicBooking.Domain.Entities;

public sealed class Clinic : Entity<Guid>, IAuditableEntity, IMustHaveTenant
{
    public Guid TenantId { get; private set; }
    public string NameEn { get; private set; } = default!;
    public string NameAr { get; private set; } = default!;
    public string AddressEn { get; private set; } = default!;
    public string AddressAr { get; private set; } = default!;
    public string CityEn { get; private set; } = default!;
    public string CityAr { get; private set; } = default!;
    public PhoneNumber PhoneNumber { get; private set; } = default!;
    public bool IsActive { get; private set; }

    public DateTime? SubscriptionStartDateUtc { get; private set; }
    public DateTime? SubscriptionEndDateUtc { get; private set; }
    public string? SubscriptionPlan { get; private set; }

    // Private Clinic & Feature Flag Settings
    public bool IsPrivateClinic { get; private set; } = true;
    public bool IsSingleDoctorClinic { get; private set; } = true;
    public bool EnableProceduresDuringShifts { get; private set; } = true;
    public bool EnableGeneralClinicFeatures { get; private set; } = false;
    public decimal ConsultationFee { get; private set; } = 350m;
    public decimal FollowUpFee { get; private set; } = 100m;
    public decimal ProcedureFee { get; private set; } = 1200m;

    public bool IsSubscriptionActive =>
        IsActive && (!SubscriptionEndDateUtc.HasValue || SubscriptionEndDateUtc.Value >= DateTime.UtcNow);

    public int? SubscriptionDaysRemaining =>
        SubscriptionEndDateUtc.HasValue
            ? Math.Max(0, (int)Math.Ceiling((SubscriptionEndDateUtc.Value - DateTime.UtcNow).TotalDays))
            : null;

    /// <summary>
    /// Default display name (English).
    /// </summary>
    public string Name => NameEn;

    /// <summary>
    /// Default display address (English).
    /// </summary>
    public string Address => AddressEn;

    /// <summary>
    /// Default display city (English).
    /// </summary>
    public string City => CityEn;

    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModifiedAtUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    private Clinic() { }

    public Clinic(
        Guid id,
        Guid tenantId,
        string nameEn,
        string addressEn,
        string cityEn,
        PhoneNumber phoneNumber,
        bool isActive = true,
        string? nameAr = null,
        string? addressAr = null,
        string? cityAr = null) : base(id)
    {
        if (tenantId == Guid.Empty) throw new DomainValidationException("Tenant ID cannot be empty.");
        TenantId = tenantId;
        SetName(nameEn, nameAr);
        SetAddress(addressEn, addressAr);
        SetCity(cityEn, cityAr);
        PhoneNumber = phoneNumber ?? throw new ArgumentNullException(nameof(phoneNumber));
        IsActive = isActive;
    }

    public static Clinic Create(
        Guid tenantId,
        string nameEn,
        string addressEn,
        string cityEn,
        string phoneNumber,
        string? nameAr = null,
        string? addressAr = null,
        string? cityAr = null)
    {
        return new Clinic(
            Guid.NewGuid(),
            tenantId,
            nameEn,
            addressEn,
            cityEn,
            PhoneNumber.Create(phoneNumber),
            true,
            nameAr,
            addressAr,
            cityAr);
    }

    public void SetName(string nameEn, string? nameAr = null)
    {
        if (string.IsNullOrWhiteSpace(nameEn))
        {
            throw new DomainValidationException("Clinic name cannot be empty.");
        }

        NameEn = nameEn.Trim();
        NameAr = string.IsNullOrWhiteSpace(nameAr) ? NameEn : nameAr.Trim();
    }

    public void SetAddress(string addressEn, string? addressAr = null)
    {
        AddressEn = addressEn?.Trim() ?? string.Empty;
        AddressAr = string.IsNullOrWhiteSpace(addressAr) ? AddressEn : addressAr.Trim();
    }

    public void SetCity(string cityEn, string? cityAr = null)
    {
        CityEn = cityEn?.Trim() ?? string.Empty;
        CityAr = string.IsNullOrWhiteSpace(cityAr) ? CityEn : cityAr.Trim();
    }

    public void SetSubscription(DateTime? startDateUtc, DateTime? endDateUtc, string? plan = "Enterprise")
    {
        if (startDateUtc.HasValue && endDateUtc.HasValue && endDateUtc.Value < startDateUtc.Value)
        {
            throw new DomainValidationException("Subscription end date cannot be earlier than start date.");
        }

        SubscriptionStartDateUtc = startDateUtc;
        SubscriptionEndDateUtc = endDateUtc;
        SubscriptionPlan = string.IsNullOrWhiteSpace(plan) ? "Enterprise" : plan.Trim();
    }

    public void SetOperatingModeAndPricing(
        bool? isPrivateClinic = null,
        bool? isSingleDoctorClinic = null,
        bool? enableProceduresDuringShifts = null,
        bool? enableGeneralClinicFeatures = null,
        decimal? consultationFee = null,
        decimal? followUpFee = null,
        decimal? procedureFee = null)
    {
        if (isPrivateClinic.HasValue) IsPrivateClinic = isPrivateClinic.Value;
        if (isSingleDoctorClinic.HasValue) IsSingleDoctorClinic = isSingleDoctorClinic.Value;
        if (enableProceduresDuringShifts.HasValue) EnableProceduresDuringShifts = enableProceduresDuringShifts.Value;
        if (enableGeneralClinicFeatures.HasValue) EnableGeneralClinicFeatures = enableGeneralClinicFeatures.Value;
        if (consultationFee.HasValue && consultationFee.Value >= 0) ConsultationFee = consultationFee.Value;
        if (followUpFee.HasValue && followUpFee.Value >= 0) FollowUpFee = followUpFee.Value;
        if (procedureFee.HasValue && procedureFee.Value >= 0) ProcedureFee = procedureFee.Value;
    }

    public void Update(
        string nameEn,
        string addressEn,
        string cityEn,
        PhoneNumber phoneNumber,
        bool isActive,
        string? nameAr = null,
        string? addressAr = null,
        string? cityAr = null,
        DateTime? subscriptionStartDateUtc = null,
        DateTime? subscriptionEndDateUtc = null,
        string? subscriptionPlan = null,
        bool? isPrivateClinic = null,
        bool? isSingleDoctorClinic = null,
        bool? enableProceduresDuringShifts = null,
        bool? enableGeneralClinicFeatures = null,
        decimal? consultationFee = null,
        decimal? followUpFee = null,
        decimal? procedureFee = null)
    {
        SetName(nameEn, nameAr);
        SetAddress(addressEn, addressAr);
        SetCity(cityEn, cityAr);
        PhoneNumber = phoneNumber ?? throw new ArgumentNullException(nameof(phoneNumber));
        IsActive = isActive;

        if (subscriptionStartDateUtc.HasValue || subscriptionEndDateUtc.HasValue || !string.IsNullOrWhiteSpace(subscriptionPlan))
        {
            SetSubscription(subscriptionStartDateUtc, subscriptionEndDateUtc, subscriptionPlan);
        }

        SetOperatingModeAndPricing(
            isPrivateClinic,
            isSingleDoctorClinic,
            enableProceduresDuringShifts,
            enableGeneralClinicFeatures,
            consultationFee,
            followUpFee,
            procedureFee);
    }

    public string GetLocalizedName(bool isArabic) => isArabic && !string.IsNullOrWhiteSpace(NameAr) ? NameAr : NameEn;
    public string GetLocalizedAddress(bool isArabic) => isArabic && !string.IsNullOrWhiteSpace(AddressAr) ? AddressAr : AddressEn;
    public string GetLocalizedCity(bool isArabic) => isArabic && !string.IsNullOrWhiteSpace(CityAr) ? CityAr : CityEn;

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
