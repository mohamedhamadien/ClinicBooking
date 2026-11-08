using ClinicBooking.Domain.Common;
using ClinicBooking.Domain.Exceptions;

namespace ClinicBooking.Domain.Entities;

public sealed class Specialty : Entity<Guid>, IAuditableEntity, IMayHaveTenant
{
    public Guid? TenantId { get; private set; }
    public string NameEn { get; private set; } = default!;
    public string NameAr { get; private set; } = default!;
    public string DescriptionEn { get; private set; } = default!;
    public string DescriptionAr { get; private set; } = default!;
    public bool IsActive { get; private set; }

    /// <summary>
    /// Default display name (English).
    /// </summary>
    public string Name => NameEn;

    /// <summary>
    /// Default display description (English).
    /// </summary>
    public string Description => DescriptionEn;

    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModifiedAtUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    private Specialty() { }

    public Specialty(
        Guid id,
        string nameEn,
        string descriptionEn,
        Guid? tenantId = null,
        bool isActive = true,
        string? nameAr = null,
        string? descriptionAr = null) : base(id)
    {
        TenantId = tenantId;
        SetName(nameEn, nameAr);
        SetDescription(descriptionEn, descriptionAr);
        IsActive = isActive;
    }

    public static Specialty Create(
        string nameEn,
        string descriptionEn,
        Guid? tenantId = null,
        string? nameAr = null,
        string? descriptionAr = null)
    {
        return new Specialty(Guid.NewGuid(), nameEn, descriptionEn, tenantId, true, nameAr, descriptionAr);
    }

    public void SetName(string nameEn, string? nameAr = null)
    {
        if (string.IsNullOrWhiteSpace(nameEn))
        {
            throw new DomainValidationException("Specialty name in English cannot be empty.");
        }

        NameEn = nameEn.Trim();
        NameAr = string.IsNullOrWhiteSpace(nameAr) ? NameEn : nameAr.Trim();
    }

    public void SetDescription(string descriptionEn, string? descriptionAr = null)
    {
        DescriptionEn = descriptionEn?.Trim() ?? string.Empty;
        DescriptionAr = string.IsNullOrWhiteSpace(descriptionAr) ? DescriptionEn : descriptionAr.Trim();
    }

    public void Update(
        string nameEn,
        string descriptionEn,
        bool isActive,
        string? nameAr = null,
        string? descriptionAr = null)
    {
        SetName(nameEn, nameAr);
        SetDescription(descriptionEn, descriptionAr);
        IsActive = isActive;
    }

    public string GetLocalizedName(bool isArabic) => isArabic && !string.IsNullOrWhiteSpace(NameAr) ? NameAr : NameEn;
    public string GetLocalizedDescription(bool isArabic) => isArabic && !string.IsNullOrWhiteSpace(DescriptionAr) ? DescriptionAr : DescriptionEn;

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
