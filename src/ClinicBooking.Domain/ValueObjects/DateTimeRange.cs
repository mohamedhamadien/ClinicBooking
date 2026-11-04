using ClinicBooking.Domain.Common;
using ClinicBooking.Domain.Exceptions;

namespace ClinicBooking.Domain.ValueObjects;

public sealed class DateTimeRange : ValueObject
{
    public DateTime StartUtc { get; }
    public DateTime EndUtc { get; }

    public TimeSpan Duration => EndUtc - StartUtc;

    private DateTimeRange(DateTime startUtc, DateTime endUtc)
    {
        StartUtc = DateTime.SpecifyKind(startUtc, DateTimeKind.Utc);
        EndUtc = DateTime.SpecifyKind(endUtc, DateTimeKind.Utc);
    }

    public static DateTimeRange Create(DateTime startUtc, DateTime endUtc)
    {
        var normalizedStart = startUtc.Kind == DateTimeKind.Utc ? startUtc : startUtc.ToUniversalTime();
        var normalizedEnd = endUtc.Kind == DateTimeKind.Utc ? endUtc : endUtc.ToUniversalTime();

        if (normalizedEnd <= normalizedStart)
        {
            throw new InvalidDateTimeRangeException(normalizedStart, normalizedEnd);
        }

        return new DateTimeRange(normalizedStart, normalizedEnd);
    }

    public static DateTimeRange FromDuration(DateTime startUtc, TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
        {
            throw new InvalidDateTimeRangeException("Duration must be strictly positive.");
        }

        return Create(startUtc, startUtc.Add(duration));
    }

    public bool OverlapsWith(DateTimeRange other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return StartUtc < other.EndUtc && EndUtc > other.StartUtc;
    }

    public bool Contains(DateTime dateTimeUtc)
    {
        var normalized = dateTimeUtc.Kind == DateTimeKind.Utc ? dateTimeUtc : dateTimeUtc.ToUniversalTime();
        return normalized >= StartUtc && normalized < EndUtc;
    }

    public bool Contains(DateTimeRange other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return StartUtc <= other.StartUtc && EndUtc >= other.EndUtc;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return StartUtc;
        yield return EndUtc;
    }

    public override string ToString() => $"{StartUtc:O} - {EndUtc:O}";
}
