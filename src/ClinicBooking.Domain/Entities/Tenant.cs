using System.Text.RegularExpressions;
using ClinicBooking.Domain.Common;
using ClinicBooking.Domain.Exceptions;

namespace ClinicBooking.Domain.Entities;

public sealed partial class Tenant : AggregateRoot<Guid>, IAuditableEntity
{
    public string Name { get; private set; } = default!;
    public string Identifier { get; private set; } = default!;
    public bool IsActive { get; private set; }
    public int CancellationNoticeHours { get; private set; }
    public bool RequiresDoctorApproval { get; private set; }

    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModifiedAtUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    [GeneratedRegex(@"^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.Compiled)]
    private static partial Regex SlugRegex();

    private Tenant() { }

    public Tenant(
        Guid id,
        string name,
        string identifier,
        bool isActive = true,
        int cancellationNoticeHours = 24,
        bool requiresDoctorApproval = false) : base(id)
    {
        SetName(name);
        SetIdentifier(identifier);
        IsActive = isActive;
        SetCancellationNoticeHours(cancellationNoticeHours);
        RequiresDoctorApproval = requiresDoctorApproval;
    }

    public static Tenant Create(
        string name,
        string identifier,
        int cancellationNoticeHours = 24,
        bool requiresDoctorApproval = false)
    {
        return new Tenant(
            Guid.NewGuid(),
            name,
            identifier,
            true,
            cancellationNoticeHours,
            requiresDoctorApproval);
    }

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainValidationException("Tenant name cannot be empty.");
        }

        Name = name.Trim();
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

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
