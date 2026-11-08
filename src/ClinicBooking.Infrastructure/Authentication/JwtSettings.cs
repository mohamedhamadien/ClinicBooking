using System.ComponentModel.DataAnnotations;

namespace ClinicBooking.Infrastructure.Authentication;

public sealed class JwtSettings
{
    public const string SectionName = "JwtSettings";

    [Required]
    [MinLength(32, ErrorMessage = "Secret key must be at least 32 characters long for HMAC-SHA256.")]
    public string SecretKey { get; set; } = default!;

    [Required]
    public string Issuer { get; set; } = default!;

    [Required]
    public string Audience { get; set; } = default!;

    [Range(1, 1440, ErrorMessage = "Expiry minutes must be between 1 and 1440.")]
    public int ExpiryMinutes { get; set; } = 60;

    [Range(1, 365, ErrorMessage = "Refresh token expiry days must be between 1 and 365.")]
    public int RefreshTokenExpiryDays { get; set; } = 7;
}
