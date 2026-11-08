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

    public void Update(
        string nameEn,
        string addressEn,
        string cityEn,
        PhoneNumber phoneNumber,
        bool isActive,
        string? nameAr = null,
        string? addressAr = null,
        string? cityAr = null)
    {
        SetName(nameEn, nameAr);
        SetAddress(addressEn, addressAr);
        SetCity(cityEn, cityAr);
        PhoneNumber = phoneNumber ?? throw new ArgumentNullException(nameof(phoneNumber));
        IsActive = isActive;
    }

    public string GetLocalizedName(bool isArabic) => isArabic && !string.IsNullOrWhiteSpace(NameAr) ? NameAr : NameEn;
    public string GetLocalizedAddress(bool isArabic) => isArabic && !string.IsNullOrWhiteSpace(AddressAr) ? AddressAr : AddressEn;
    public string GetLocalizedCity(bool isArabic) => isArabic && !string.IsNullOrWhiteSpace(CityAr) ? CityAr : CityEn;

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
