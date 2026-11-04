using System.Text.RegularExpressions;
using ClinicBooking.Domain.Common;
using ClinicBooking.Domain.Exceptions;

namespace ClinicBooking.Domain.ValueObjects;

public sealed partial class EmailAddress : ValueObject
{
    public string Value { get; }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex EmailRegex();

    private EmailAddress(string value)
    {
        Value = value;
    }

    public static EmailAddress Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException("Email address cannot be empty.");
        }

        var trimmed = value.Trim().ToLowerInvariant();
        if (!EmailRegex().IsMatch(trimmed))
        {
            throw new DomainValidationException($"Email address '{value}' is not valid.");
        }

        return new EmailAddress(trimmed);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(EmailAddress email) => email.Value;
}
