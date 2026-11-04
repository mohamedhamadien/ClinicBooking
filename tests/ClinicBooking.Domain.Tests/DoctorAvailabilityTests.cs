using ClinicBooking.Domain.Entities;
using ClinicBooking.Domain.Exceptions;
using ClinicBooking.Domain.ValueObjects;

namespace ClinicBooking.Domain.Tests;

public class DoctorAvailabilityTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _specialtyId = Guid.NewGuid();
    private readonly Guid _clinicId = Guid.NewGuid();

    [Fact]
    public void AddAvailability_WithValidShift_AddsToAvailabilities()
    {
        // Arrange
        var doctor = Doctor.Create(_userId, _specialtyId, "DOC-12345", "Cardiologist");

        // Act
        doctor.AddAvailability(_clinicId, DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(13, 0));

        // Assert
        Assert.Single(doctor.Availabilities);
        var availability = doctor.Availabilities.First();
        Assert.Equal(DayOfWeek.Monday, availability.DayOfWeek);
        Assert.Equal(new TimeOnly(9, 0), availability.Shift.StartTime);
        Assert.Equal(new TimeOnly(13, 0), availability.Shift.EndTime);
        Assert.True(availability.IsActive);
    }

    [Fact]
    public void AddAvailability_WithOverlappingShiftOnSameDay_ThrowsDomainValidationException()
    {
        // Arrange
        var doctor = Doctor.Create(_userId, _specialtyId, "DOC-12345", "Cardiologist");
        doctor.AddAvailability(_clinicId, DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(13, 0));

        // Act & Assert (11:00 to 15:00 overlaps with 9:00 to 13:00)
        Assert.Throws<DomainValidationException>(() =>
            doctor.AddAvailability(_clinicId, DayOfWeek.Monday, new TimeOnly(11, 0), new TimeOnly(15, 0)));
    }

    [Fact]
    public void AddAvailability_WithNonOverlappingShiftOnSameDay_Succeeds()
    {
        // Arrange
        var doctor = Doctor.Create(_userId, _specialtyId, "DOC-12345", "Cardiologist");
        doctor.AddAvailability(_clinicId, DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(12, 0));

        // Act (Afternoon shift 14:00 to 18:00)
        doctor.AddAvailability(_clinicId, DayOfWeek.Monday, new TimeOnly(14, 0), new TimeOnly(18, 0));

        // Assert
        Assert.Equal(2, doctor.Availabilities.Count);
    }

    [Fact]
    public void AddBlackoutException_AndIsBlockedAt_DetectsBlackoutCorrectly()
    {
        // Arrange
        var doctor = Doctor.Create(_userId, _specialtyId, "DOC-12345", "Cardiologist");
        var baseDate = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc);
        var vacationStart = baseDate.AddHours(8);
        var vacationEnd = baseDate.AddHours(17);

        // Act
        doctor.AddBlackoutException(vacationStart, vacationEnd, "Annual Vacation");

        // Assert
        var overlappingQuery = DateTimeRange.Create(baseDate.AddHours(10), baseDate.AddHours(11));
        var outsideQuery = DateTimeRange.Create(baseDate.AddHours(18), baseDate.AddHours(19));

        Assert.True(doctor.IsBlockedAt(overlappingQuery));
        Assert.False(doctor.IsBlockedAt(outsideQuery));
    }

    [Theory]
    [InlineData(5)]
    [InlineData(181)]
    [InlineData(-10)]
    public void SetSlotDuration_WithInvalidMinutes_ThrowsSlotDurationInvalidException(int minutes)
    {
        // Arrange
        var doctor = Doctor.Create(_userId, _specialtyId, "DOC-12345", "Cardiologist");

        // Act & Assert
        Assert.Throws<SlotDurationInvalidException>(() => doctor.SetSlotDuration(minutes));
    }

    [Theory]
    [InlineData(15)]
    [InlineData(30)]
    [InlineData(45)]
    [InlineData(60)]
    public void SetSlotDuration_WithValidMinutes_UpdatesDuration(int minutes)
    {
        // Arrange
        var doctor = Doctor.Create(_userId, _specialtyId, "DOC-12345", "Cardiologist");

        // Act
        doctor.SetSlotDuration(minutes);

        // Assert
        Assert.Equal(minutes, doctor.DefaultSlotDurationMinutes);
    }
}
