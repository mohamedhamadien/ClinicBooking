using ClinicBooking.Domain.Common;

namespace ClinicBooking.Domain.Entities;

public sealed class RefreshToken : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public string Token { get; private set; } = default!;
    public string JwtId { get; private set; } = default!;
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public string? CreatedByIp { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }
    public string? RevokedByIp { get; private set; }
    public string? ReplacedByToken { get; private set; }
    public string? ReasonRevoked { get; private set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;
    public bool IsRevoked => RevokedAtUtc is not null;
    public bool IsActive => !IsRevoked && !IsExpired;

    private RefreshToken() { }

    public RefreshToken(
        Guid id,
        Guid userId,
        string token,
        string jwtId,
        DateTime expiresAtUtc,
        DateTime createdAtUtc,
        string? createdByIp) : base(id)
    {
        UserId = userId;
        Token = token;
        JwtId = jwtId;
        ExpiresAtUtc = DateTime.SpecifyKind(expiresAtUtc, DateTimeKind.Utc);
        CreatedAtUtc = DateTime.SpecifyKind(createdAtUtc, DateTimeKind.Utc);
        CreatedByIp = createdByIp;
    }

    public void Revoke(DateTime revokedAtUtc, string? revokedByIp, string? reason, string? replacedByToken = null)
    {
        RevokedAtUtc = DateTime.SpecifyKind(revokedAtUtc, DateTimeKind.Utc);
        RevokedByIp = revokedByIp;
        ReasonRevoked = reason;
        ReplacedByToken = replacedByToken;
    }
}
