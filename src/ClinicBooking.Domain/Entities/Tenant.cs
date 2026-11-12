using System.Text.RegularExpressions;
using ClinicBooking.Domain.Common;
using ClinicBooking.Domain.Exceptions;

namespace ClinicBooking.Domain.Entities;

public sealed partial class Tenant : AggregateRoot<Guid>, IAuditableEntity
{
    public string NameEn { get; private set; } = default!;
    public string NameAr { get; private set; } = default!;
    public string Identifier { get; private set; } = default!;
    public bool IsActive { get; private set; }
    public int CancellationNoticeHours { get; private set; }
    public bool RequiresDoctorApproval { get; private set; }

    public DateTime? SubscriptionStartDateUtc { get; private set; }
    public DateTime? SubscriptionEndDateUtc { get; private set; }
    public string? SubscriptionPlan { get; private set; }

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

    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModifiedAtUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    [GeneratedRegex(@"^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.Compiled)]
    private static partial Regex SlugRegex();

    private Tenant() { }

    public Tenant(
        Guid id,
        string nameEn,
        string identifier,
        bool isActive = true,
        int cancellationNoticeHours = 24,
        bool requiresDoctorApproval = false,
        string? nameAr = null) : base(id)
    {
        SetName(nameEn, nameAr);
        SetIdentifier(identifier);
        IsActive = isActive;
        SetCancellationNoticeHours(cancellationNoticeHours);
        RequiresDoctorApproval = requiresDoctorApproval;
    }

    public static Tenant Create(
        string nameEn,
        string identifier,
        int cancellationNoticeHours = 24,
        bool requiresDoctorApproval = false,
        string? nameAr = null)
    {
        return new Tenant(
            Guid.NewGuid(),
            nameEn,
            identifier,
            true,
            cancellationNoticeHours,
            requiresDoctorApproval,
            nameAr);
    }

    public void SetName(string nameEn, string? nameAr = null)
    {
        if (string.IsNullOrWhiteSpace(nameEn))
        {
            throw new DomainValidationException("Tenant name cannot be empty.");
        }

        NameEn = nameEn.Trim();
        NameAr = string.IsNullOrWhiteSpace(nameAr) ? NameEn : nameAr.Trim();
    }

    public void SetIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new DomainValidationException("Tenant identifier cannot be empty.");
        }

        var normalized = identifier.Trim().ToLowerInvariant();
        if (!SlugRegex().IsMatch(normalized) || normalized.Length < 3 || normalized.Length > 50)
        {
            throw new DomainValidationException($"Tenant identifier '{identifier}' is invalid. It must be lowercase alphanumeric with hyphens, between 3 and 50 characters.");
        }

        Identifier = normalized;
    }

    public void SetCancellationNoticeHours(int hours)
    {
        if (hours < 0 || hours > 168) // between 0 hours (instant) and 7 days (168 hours)
        {
            throw new DomainValidationException("Cancellation notice hours must be between 0 and 168 hours (7 days).");
        }

        CancellationNoticeHours = hours;
    }

    public void SetDoctorApprovalPolicy(bool requiresApproval)
    {
        RequiresDoctorApproval = requiresApproval;
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

    public string GetLocalizedName(bool isArabic) => isArabic && !string.IsNullOrWhiteSpace(NameAr) ? NameAr : NameEn;

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
