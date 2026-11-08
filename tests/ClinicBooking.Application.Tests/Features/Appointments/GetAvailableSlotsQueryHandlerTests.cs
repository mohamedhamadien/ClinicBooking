using ClinicBooking.Application.Features.Appointments.Queries.GetAvailableSlots;
using ClinicBooking.Application.Tests.TestHelpers;
using ClinicBooking.Domain.Entities;
using ClinicBooking.Domain.ValueObjects;

namespace ClinicBooking.Application.Tests.Features.Appointments;

public class GetAvailableSlotsQueryHandlerTests
{
    private readonly FakeDoctorRepository _doctorRepository = new();
    private readonly FakeAppointmentRepository _appointmentRepository = new();
    private readonly FakeDateTimeProvider _dateTimeProvider = new();

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _doctorId = Guid.NewGuid();
    private readonly Guid _clinicId = Guid.NewGuid();

    public GetAvailableSlotsQueryHandlerTests()
    {
        // Doctor with 30 minute slots and Monday shift 09:00 - 12:00 (3 hours = 6 slots)
        var doctor = Doctor.Create(_tenantId, Guid.NewGuid(), Guid.NewGuid(), "DOC-100", "Cardiologist", defaultSlotDurationMinutes: 30);
        typeof(Doctor).GetProperty(nameof(Doctor.Id))!.SetValue(doctor, _doctorId);
        doctor.AddAvailability(_clinicId, DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(12, 0));
        _doctorRepository.Seed(doctor);

        // System clock: Sunday, Nov 1, 2026 12:00 UTC
        _dateTimeProvider.UtcNow = new DateTime(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc);
    }

    [Fact]
    public async Task Handle_WithShiftAndNoBookings_GeneratesCorrectSlotsCount()
    {
        // Arrange: Monday Nov 2, 2026
        var query = new GetAvailableSlotsQuery(_tenantId, _doctorId, _clinicId, new DateOnly(2026, 11, 2));
        var handler = new GetAvailableSlotsQueryHandler(_doctorRepository, _appointmentRepository, _dateTimeProvider);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(6, result.Value.Count); // 9:00, 9:30, 10:00, 10:30, 11:00, 11:30
        Assert.Equal(new DateTime(2026, 11, 2, 9, 0, 0, DateTimeKind.Utc), result.Value.First().StartUtc);
        Assert.Equal(new DateTime(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc), result.Value.Last().EndUtc);
    }

    [Fact]
    public async Task Handle_WhenSlotIsBooked_ExcludesBookedSlotFromResults()
    {
        // Arrange: Monday Nov 2, 2026 with 10:00 - 10:30 slot booked
        var bookedStart = new DateTime(2026, 11, 2, 10, 0, 0, DateTimeKind.Utc);
        var bookedEnd = new DateTime(2026, 11, 2, 10, 30, 0, DateTimeKind.Utc);

        var bookedAppointment = Appointment.Create(
            _tenantId,
            _doctorId,
            Guid.NewGuid(),
            _clinicId,
            DateTimeRange.Create(bookedStart, bookedEnd),
            _dateTimeProvider.UtcNow);

        _appointmentRepository.Seed(bookedAppointment);

        var query = new GetAvailableSlotsQuery(_tenantId, _doctorId, _clinicId, new DateOnly(2026, 11, 2));
        var handler = new GetAvailableSlotsQueryHandler(_doctorRepository, _appointmentRepository, _dateTimeProvider);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.Count); // 6 total minus 1 booked = 5
        Assert.DoesNotContain(result.Value, s => s.StartUtc == bookedStart);
    }

    [Fact]
    public async Task Handle_WhenDoctorHasBlackout_ExcludesBlockedSlots()
    {
        // Arrange: Doctor is on leave 11:00 - 12:00
        var doctor = await _doctorRepository.GetByIdWithSchedulesAsync(_doctorId);
        var blockStart = new DateTime(2026, 11, 2, 11, 0, 0, DateTimeKind.Utc);
        var blockEnd = new DateTime(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);
        doctor!.AddBlackoutException(blockStart, blockEnd, "Meeting");

        var query = new GetAvailableSlotsQuery(_tenantId, _doctorId, _clinicId, new DateOnly(2026, 11, 2));
        var handler = new GetAvailableSlotsQueryHandler(_doctorRepository, _appointmentRepository, _dateTimeProvider);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(4, result.Value.Count); // 6 minus 2 blocked (11:00 and 11:30) = 4
        Assert.DoesNotContain(result.Value, s => s.StartUtc >= blockStart && s.StartUtc < blockEnd);
    }

    [Fact]
    public async Task Handle_WhenDayHasNoWorkingShifts_ReturnsEmptyList()
    {
        // Arrange: Tuesday Nov 3, 2026 (Doctor has no shift on Tuesday)
        var query = new GetAvailableSlotsQuery(_tenantId, _doctorId, _clinicId, new DateOnly(2026, 11, 3));
        var handler = new GetAvailableSlotsQueryHandler(_doctorRepository, _appointmentRepository, _dateTimeProvider);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }
}
