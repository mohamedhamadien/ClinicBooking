using ClinicBooking.Domain.Common;
using ClinicBooking.Domain.Constants;
using ClinicBooking.Domain.Enums;
using ClinicBooking.Domain.Exceptions;
using ClinicBooking.Domain.ValueObjects;

namespace ClinicBooking.Domain.Entities;

public sealed class User : AggregateRoot<Guid>, IAuditableEntity, IMayHaveTenant
{
    public Guid? TenantId { get; private set; }
    public Guid? ClinicId { get; private set; }
    public EmailAddress Email { get; private set; } = default!;
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; }

    /// <summary>
    /// Opaque password hash produced by the infrastructure password hasher (PBKDF2 via ASP.NET Core Identity).
    /// The domain never sees plaintext passwords.
    /// </summary>
    public string PasswordHash { get; private set; } = string.Empty;

    public void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainValidationException("Password hash cannot be empty.");
        }

        PasswordHash = passwordHash;
    }

    public RefreshToken? FindRefreshToken(string token) => _refreshTokens.FirstOrDefault(t => t.Token == token);

    public void RevokeAllActiveRefreshTokens(string? revokedByIp, string reason)
    {
        foreach (var refreshToken in _refreshTokens.Where(t => t.IsActive))
        {
            refreshToken.Revoke(DateTime.UtcNow, revokedByIp, reason);
        }
    }

    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModifiedAtUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();

    private readonly List<RefreshToken> _refreshTokens = [];
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    private readonly List<UserPermission> _permissions = [];
    public IReadOnlyCollection<UserPermission> Permissions => _permissions.AsReadOnly();

    private User() { }

    public User(
        Guid id,
        EmailAddress email,
        string firstName,
        string lastName,
        UserRole role,
        Guid? tenantId = null,
        bool isActive = true,
        Guid? clinicId = null) : base(id)
    {
        TenantId = tenantId;
        ClinicId = clinicId;
        Email = email ?? throw new ArgumentNullException(nameof(email));
        SetNames(firstName, lastName);
        Role = role;
        IsActive = isActive;
    }

    public static User Create(
        string email,
        string firstName,
        string lastName,
        UserRole role,
        Guid? tenantId = null,
        Guid? clinicId = null)
    {
        return new User(
            Guid.NewGuid(),
            EmailAddress.Create(email),
            firstName,
            lastName,
            role,
            tenantId,
            true,
            clinicId);
    }

    public void AssignToClinic(Guid? clinicId)
    {
        ClinicId = clinicId;
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

    public void UpdateProfile(string firstName, string lastName)
    {
        SetNames(firstName, lastName);
    }

    public void SetRole(UserRole role)
    {
        Role = role;
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

    public void SetPermissions(IEnumerable<string> permissions)
    {
        _permissions.Clear();
        foreach (var perm in permissions.Distinct())
        {
            if (!string.IsNullOrWhiteSpace(perm))
            {
                _permissions.Add(new UserPermission(Guid.NewGuid(), Id, perm.Trim(), TenantId, DateTime.UtcNow));
            }
        }
    }

    public IReadOnlyList<string> GetEffectivePermissions()
    {
        if (Role == UserRole.Admin || Role == UserRole.ClinicAdmin)
        {
            return ClinicPermissions.All.Select(p => p.Key).ToList();
        }

        if (_permissions.Count > 0)
        {
            return _permissions.Select(p => p.Permission).Distinct().ToList();
        }

        if (ClinicPermissions.RolePresets.TryGetValue(Role.ToString(), out var preset))
        {
            return preset;
        }

        return [];
    }

    public bool HasPermission(string permission)
    {
        return GetEffectivePermissions().Contains(permission, StringComparer.OrdinalIgnoreCase);
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
