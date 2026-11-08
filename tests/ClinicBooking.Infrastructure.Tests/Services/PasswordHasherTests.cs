using ClinicBooking.Infrastructure.Services;

namespace ClinicBooking.Infrastructure.Tests.Services;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void HashPassword_ProducesNonEmptyHashedString()
    {
        // Act
        var hash = _hasher.HashPassword("SuperSecretP@ssw0rd");

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(hash));
        Assert.NotEqual("SuperSecretP@ssw0rd", hash);
    }

    [Fact]
    public void HashPassword_ProducesDifferentHashesForSamePasswordDueToSalting()
    {
        // Act
        var hash1 = _hasher.HashPassword("TestP@ssw0rd1");
        var hash2 = _hasher.HashPassword("TestP@ssw0rd1");

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ReturnsTrue()
    {
        // Arrange
        var password = "CorrectHorseBatteryStaple!";
        var hash = _hasher.HashPassword(password);

        // Act
        var isValid = _hasher.VerifyPassword(password, hash);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void VerifyPassword_WithIncorrectPassword_ReturnsFalse()
    {
        // Arrange
        var hash = _hasher.HashPassword("ValidPassword123!");

        // Act
        var isValid = _hasher.VerifyPassword("WrongPassword456!", hash);

        // Assert
        Assert.False(isValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void VerifyPassword_WithInvalidInputs_ReturnsFalse(string? invalidInput)
    {
        // Arrange
        var hash = _hasher.HashPassword("ValidPassword123!");

        // Act & Assert
        Assert.False(_hasher.VerifyPassword(invalidInput!, hash));
        Assert.False(_hasher.VerifyPassword("ValidPassword123!", invalidInput!));
    }
}
