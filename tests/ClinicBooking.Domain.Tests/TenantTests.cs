using ClinicBooking.Domain.Entities;
using ClinicBooking.Domain.Exceptions;

namespace ClinicBooking.Domain.Tests;

public class TenantTests
{
    [Fact]
    public void Create_WithValidParameters_InitializesCorrectly()
    {
        // Act
        var tenant = Tenant.Create("Apex Healthcare", "apex-health", cancellationNoticeHours: 48, requiresDoctorApproval: true);

        // Assert
        Assert.NotEqual(Guid.Empty, tenant.Id);
        Assert.Equal("Apex Healthcare", tenant.Name);
        Assert.Equal("apex-health", tenant.Identifier);
        Assert.True(tenant.IsActive);
        Assert.Equal(48, tenant.CancellationNoticeHours);
        Assert.True(tenant.RequiresDoctorApproval);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SetName_WithEmptyName_ThrowsDomainValidationException(string invalidName)
    {
        // Arrange
        var tenant = Tenant.Create("Initial Name", "initial-slug");

        // Act & Assert
        Assert.Throws<DomainValidationException>(() => tenant.SetName(invalidName));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ab")] // Too short (< 3)
    [InlineData("Invalid_Slug")] // Underscores and uppercase not permitted
    [InlineData("-invalid-slug")] // Leading hyphen
    [InlineData("invalid-slug-")] // Trailing hyphen
    [InlineData("invalid--slug")] // Double hyphen
    [InlineData("slug with spaces")]
    public void SetIdentifier_WithInvalidSlug_ThrowsDomainValidationException(string invalidSlug)
    {
        // Arrange
        var tenant = Tenant.Create("Valid Name", "valid-slug");

        // Act & Assert
        Assert.Throws<DomainValidationException>(() => tenant.SetIdentifier(invalidSlug));
    }

    [Theory]
    [InlineData("metro-health")]
    [InlineData("clinic123")]
    [InlineData("st-jude-children-hospital")]
    public void SetIdentifier_WithValidSlug_SetsNormalizedIdentifier(string validSlug)
    {
        // Arrange
        var tenant = Tenant.Create("Valid Name", "valid-slug");

        // Act
        tenant.SetIdentifier(validSlug);

        // Assert
        Assert.Equal(validSlug.ToLowerInvariant(), tenant.Identifier);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(169)] // Greater than 7 days
    public void SetCancellationNoticeHours_WithOutOfBounds_ThrowsDomainValidationException(int hours)
    {
        // Arrange
        var tenant = Tenant.Create("Valid Name", "valid-slug");

        // Act & Assert
        Assert.Throws<DomainValidationException>(() => tenant.SetCancellationNoticeHours(hours));
    }

    [Fact]
    public void Deactivate_SetsIsActiveToFalse()
    {
        // Arrange
        var tenant = Tenant.Create("Valid Name", "valid-slug");

        // Act
        tenant.Deactivate();

        // Assert
        Assert.False(tenant.IsActive);

        // Act: reactivate
        tenant.Activate();
        Assert.True(tenant.IsActive);
    }
}
