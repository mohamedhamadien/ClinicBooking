using ClinicBooking.Domain.Common;
using ClinicBooking.Domain.Enums;
using ClinicBooking.Domain.Exceptions;
using ClinicBooking.Domain.ValueObjects;

namespace ClinicBooking.Domain.Entities;

public sealed class User : AggregateRoot<Guid>, IAuditableEntity, IMayHaveTenant
{
    public Guid? TenantId { get; private set; }
    public EmailAddress Email { get; private set; } = default!;
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; }

    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModifiedAtUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();

    private readonly List<RefreshToken> _refreshTokens = [];
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    private User() { }

    public User(
        Guid id,
        EmailAddress email,
        string firstName,
        string lastName,
        UserRole role,
        Guid? tenantId = null,
        bool isActive = true) : base(id)
    {
        TenantId = tenantId;
        Email = email ?? throw new ArgumentNullException(nameof(email));
        SetNames(firstName, lastName);
        Role = role;
        IsActive = isActive;
    }

    public static User Create(string email, string firstName, string lastName, UserRole role, Guid? tenantId = null)
    {
        return new User(
            Guid.NewGuid(),
            EmailAddress.Create(email),
            firstName,
            lastName,
            role,
            tenantId,
            true);
    }

    public void SetNames(string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new DomainValidationException("First name cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new DomainValidationException("Last name cannot be empty.");
        }

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;

    public void AddRefreshToken(string token, string jwtId, DateTime expiresAtUtc, string? createdByIp)
    {
        _refreshTokens.Add(new RefreshToken(
            Guid.NewGuid(),
            Id,
            token,
            jwtId,
            expiresAtUtc,
            DateTime.UtcNow,
            createdByIp));
    }

    public void RevokeRefreshToken(string token, string? revokedByIp, string? reason, string? replacedByToken = null)
    {
        var existing = _refreshTokens.FirstOrDefault(x => x.Token == token);
        if (existing is not null && existing.IsActive)
        {
            existing.Revoke(DateTime.UtcNow, revokedByIp, reason, replacedByToken);
        }
    }
}
