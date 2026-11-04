using ClinicBooking.Domain.Exceptions;
using ClinicBooking.Domain.ValueObjects;

namespace ClinicBooking.Domain.Tests;

public class DateTimeRangeTests
{
    [Fact]
    public void Create_WithValidDates_SetsUtcProperties()
    {
        // Arrange
        var start = DateTime.UtcNow.AddHours(1);
        var end = start.AddHours(1);

        // Act
        var range = DateTimeRange.Create(start, end);

        // Assert
        Assert.Equal(start, range.StartUtc);
        Assert.Equal(end, range.EndUtc);
        Assert.Equal(TimeSpan.FromHours(1), range.Duration);
        Assert.Equal(DateTimeKind.Utc, range.StartUtc.Kind);
        Assert.Equal(DateTimeKind.Utc, range.EndUtc.Kind);
    }

    [Fact]
    public void Create_WithInvertedDates_ThrowsInvalidDateTimeRangeException()
    {
        // Arrange
        var start = DateTime.UtcNow.AddHours(2);
        var end = DateTime.UtcNow.AddHours(1);

        // Act & Assert
        Assert.Throws<InvalidDateTimeRangeException>(() => DateTimeRange.Create(start, end));
    }

    [Fact]
    public void Create_WithEqualDates_ThrowsInvalidDateTimeRangeException()
    {
        // Arrange
        var date = DateTime.UtcNow.AddHours(1);

        // Act & Assert
        Assert.Throws<InvalidDateTimeRangeException>(() => DateTimeRange.Create(date, date));
    }

    [Fact]
    public void OverlapsWith_WhenRangesOverlap_ReturnsTrue()
    {
        // Arrange
        var baseTime = DateTime.UtcNow.AddDays(1);
        var rangeA = DateTimeRange.Create(baseTime.AddHours(10), baseTime.AddHours(12));
        var rangeB = DateTimeRange.Create(baseTime.AddHours(11), baseTime.AddHours(13));

        // Act & Assert
        Assert.True(rangeA.OverlapsWith(rangeB));
        Assert.True(rangeB.OverlapsWith(rangeA));
    }

    [Fact]
    public void OverlapsWith_WhenRangesAreAbutting_ReturnsFalse()
    {
        // Arrange
        var baseTime = DateTime.UtcNow.AddDays(1);
        var rangeA = DateTimeRange.Create(baseTime.AddHours(10), baseTime.AddHours(11));
        var rangeB = DateTimeRange.Create(baseTime.AddHours(11), baseTime.AddHours(12));

        // Act & Assert
        Assert.False(rangeA.OverlapsWith(rangeB));
        Assert.False(rangeB.OverlapsWith(rangeA));
    }

    [Fact]
    public void OverlapsWith_WhenRangesAreDisjoint_ReturnsFalse()
    {
        // Arrange
        var baseTime = DateTime.UtcNow.AddDays(1);
        var rangeA = DateTimeRange.Create(baseTime.AddHours(9), baseTime.AddHours(10));
        var rangeB = DateTimeRange.Create(baseTime.AddHours(14), baseTime.AddHours(15));

        // Act & Assert
        Assert.False(rangeA.OverlapsWith(rangeB));
        Assert.False(rangeB.OverlapsWith(rangeA));
    }

    [Fact]
    public void Contains_WhenDateTimeIsInside_ReturnsTrue()
    {
        // Arrange
        var baseTime = DateTime.UtcNow.AddDays(1);
        var range = DateTimeRange.Create(baseTime.AddHours(10), baseTime.AddHours(12));

        // Act & Assert
        Assert.True(range.Contains(baseTime.AddHours(10)));
        Assert.True(range.Contains(baseTime.AddHours(11)));
        Assert.False(range.Contains(baseTime.AddHours(12))); // Exclusive upper bound
        Assert.False(range.Contains(baseTime.AddHours(9)));
    }

    [Fact]
    public void Contains_WhenRangeIsCompletelyInside_ReturnsTrue()
    {
        // Arrange
        var baseTime = DateTime.UtcNow.AddDays(1);
        var outer = DateTimeRange.Create(baseTime.AddHours(9), baseTime.AddHours(15));
        var inner = DateTimeRange.Create(baseTime.AddHours(10), baseTime.AddHours(12));

        // Act & Assert
        Assert.True(outer.Contains(inner));
        Assert.False(inner.Contains(outer));
    }
}
