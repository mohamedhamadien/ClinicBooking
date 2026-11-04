using ClinicBooking.Domain.Common;
using ClinicBooking.Domain.Exceptions;

namespace ClinicBooking.Domain.ValueObjects;

public sealed class TimeSlot : ValueObject
{
    public TimeOnly StartTime { get; }
    public TimeOnly EndTime { get; }

    public TimeSpan Duration => EndTime - StartTime;

    private TimeSlot(TimeOnly startTime, TimeOnly endTime)
    {
        StartTime = startTime;
        EndTime = endTime;
    }

    public static TimeSlot Create(TimeOnly startTime, TimeOnly endTime)
    {
        if (endTime <= startTime)
        {
            throw new InvalidDateTimeRangeException($"Invalid time slot: EndTime ({endTime}) must be after StartTime ({startTime}).");
        }

        return new TimeSlot(startTime, endTime);
    }

    public bool OverlapsWith(TimeSlot other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return StartTime < other.EndTime && EndTime > other.StartTime;
    }

    public bool Contains(TimeOnly time)
    {
        return time >= StartTime && time < EndTime;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return StartTime;
        yield return EndTime;
    }

    public override string ToString() => $"{StartTime:HH:mm} - {EndTime:HH:mm}";
}
