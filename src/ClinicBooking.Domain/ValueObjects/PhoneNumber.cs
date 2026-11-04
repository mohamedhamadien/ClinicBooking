using System.Text.RegularExpressions;
using ClinicBooking.Domain.Common;
using ClinicBooking.Domain.Exceptions;

namespace ClinicBooking.Domain.ValueObjects;

public sealed partial class PhoneNumber : ValueObject
{
    public string Value { get; }

    [GeneratedRegex(@"^\+?[0-9\s\-\(\)]{7,20}$", RegexOptions.Compiled)]
    private static partial Regex PhoneRegex();

    private PhoneNumber(string value)
    {
        Value = value;
    }

    public static PhoneNumber Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException("Phone number cannot be empty.");
        }

        var trimmed = value.Trim();
        if (!PhoneRegex().IsMatch(trimmed))
        {
            throw new DomainValidationException($"Phone number '{value}' is not valid.");
        }

        return new PhoneNumber(trimmed);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(PhoneNumber phone) => phone.Value;
}
