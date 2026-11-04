using ClinicBooking.Domain.Exceptions;
using ClinicBooking.Domain.ValueObjects;

namespace ClinicBooking.Domain.Tests;

public class ValueObjectTests
{
    [Fact]
    public void TimeSlot_Create_WithEndBeforeStart_ThrowsInvalidDateTimeRangeException()
    {
        // Act & Assert
        Assert.Throws<InvalidDateTimeRangeException>(() =>
            TimeSlot.Create(new TimeOnly(14, 0), new TimeOnly(10, 0)));
    }

    [Fact]
    public void TimeSlot_OverlapsWith_CalculatesAccurately()
    {
        // Arrange
        var slotA = TimeSlot.Create(new TimeOnly(9, 0), new TimeOnly(12, 0));
        var slotB = TimeSlot.Create(new TimeOnly(11, 0), new TimeOnly(14, 0));
        var slotC = TimeSlot.Create(new TimeOnly(12, 0), new TimeOnly(15, 0)); // abutting

        // Act & Assert
        Assert.True(slotA.OverlapsWith(slotB));
        Assert.False(slotA.OverlapsWith(slotC)); // Abutting is not overlapping
    }

    [Fact]
    public void PhoneNumber_Create_WithValidFormat_ReturnsValue()
    {
        // Arrange
        var raw = "+1-555-123-4567";

        // Act
        var phone = PhoneNumber.Create(raw);

        // Assert
        Assert.Equal(raw, phone.Value);
        Assert.Equal(raw, (string)phone);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-number")]
    [InlineData("12")]
    public void PhoneNumber_Create_WithInvalidFormat_ThrowsDomainValidationException(string invalid)
    {
        // Act & Assert
        Assert.Throws<DomainValidationException>(() => PhoneNumber.Create(invalid));
    }

    [Fact]
    public void EmailAddress_Create_WithValidFormat_NormalizesToLowercase()
    {
        // Arrange
        var raw = "Patient.User@Example.COM";

        // Act
        var email = EmailAddress.Create(raw);

        // Assert
        Assert.Equal("patient.user@example.com", email.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid-email")]
    [InlineData("missing@domain")]
    public void EmailAddress_Create_WithInvalidFormat_ThrowsDomainValidationException(string invalid)
    {
        // Act & Assert
        Assert.Throws<DomainValidationException>(() => EmailAddress.Create(invalid));
    }

    [Fact]
    public void ValueObjects_WithIdenticalValues_AreEqual()
    {
        // Arrange
        var email1 = EmailAddress.Create("test@example.com");
        var email2 = EmailAddress.Create("test@example.com");
        var slot1 = TimeSlot.Create(new TimeOnly(9, 0), new TimeOnly(17, 0));
        var slot2 = TimeSlot.Create(new TimeOnly(9, 0), new TimeOnly(17, 0));

        // Act & Assert
        Assert.Equal(email1, email2);
        Assert.True(email1 == email2);
        Assert.Equal(slot1, slot2);
        Assert.True(slot1 == slot2);
        Assert.Equal(email1.GetHashCode(), email2.GetHashCode());
        Assert.Equal(slot1.GetHashCode(), slot2.GetHashCode());
    }
}
