using ClinicBooking.Application.Features.Doctors.Commands.AddAvailabilityException;
using ClinicBooking.Application.Features.Doctors.Commands.RemoveAvailabilityException;
using ClinicBooking.Application.Features.Doctors.Commands.RemoveDoctorAvailability;
using ClinicBooking.Application.Features.Doctors.Commands.SetDoctorAvailability;
using ClinicBooking.Application.Tests.TestHelpers;
using ClinicBooking.Domain.Entities;
using ClinicBooking.Domain.Enums;
using Xunit;

namespace ClinicBooking.Application.Tests.Features.Doctors;

public class DoctorScheduleTests
{
    private readonly FakeDoctorRepository _doctorRepo = new();
    private readonly FakeClinicRepository _clinicRepo = new();
    private readonly FakeTenantRepository _tenantRepo = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private readonly Tenant _tenant;
    private readonly Clinic _clinic;
    private readonly Doctor _doctor;

    public DoctorScheduleTests()
    {
        _tenant = Tenant.Create("Apex Network", "apex", 24, false);
        _tenantRepo.Seed(_tenant);

        _clinic = Clinic.Create(_tenant.Id, "Apex Main", "Address", "Cairo", "+201000000000");
        _clinicRepo.Seed(_clinic);

        _doctor = Doctor.Create(
            _tenant.Id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "LIC-DOC-999",
            "Consultant Physician",
            30);

        _doctorRepo.Seed(_doctor);
    }

    [Fact]
    public async Task SetDoctorAvailability_Should_Add_Shift_Including_Weekend_Friday()
    {
        // Arrange
        var handler = new SetDoctorAvailabilityCommandHandler(_doctorRepo, _clinicRepo, _unitOfWork);
        var command = new SetDoctorAvailabilityCommand(
            _doctor.Id,
            _clinic.Id,
            DayOfWeek.Friday, // Weekend work
            new TimeOnly(14, 0),
            new TimeOnly(20, 0));

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(_doctor.Availabilities);
        var shift = _doctor.Availabilities.First();
        Assert.Equal(DayOfWeek.Friday, shift.DayOfWeek);
        Assert.Equal(new TimeOnly(14, 0), shift.Shift.StartTime);
        Assert.Equal(new TimeOnly(20, 0), shift.Shift.EndTime);
    }

    [Fact]
    public async Task RemoveDoctorAvailability_Should_Delete_Shift()
    {
        // Arrange
        _doctor.AddAvailability(_clinic.Id, DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(13, 0));
        var availId = _doctor.Availabilities.First().Id;

        var handler = new RemoveDoctorAvailabilityCommandHandler(_doctorRepo, _unitOfWork);
        var command = new RemoveDoctorAvailabilityCommand(_doctor.Id, availId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(_doctor.Availabilities);
    }

    [Fact]
    public async Task AddAvailabilityException_Should_Record_Official_Holiday_Block()
    {
        // Arrange
        var handler = new AddAvailabilityExceptionCommandHandler(_doctorRepo, _unitOfWork);
        var startUtc = DateTime.UtcNow.Date.AddDays(1);
        var endUtc = startUtc.AddDays(3);
        var command = new AddAvailabilityExceptionCommand(_doctor.Id, startUtc, endUtc, "Eid Official Holiday");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(_doctor.Exceptions);
        var ex = _doctor.Exceptions.First();
        Assert.Equal("Eid Official Holiday", ex.Reason);
        Assert.True(ex.IsBlocked);
    }

    [Fact]
    public async Task RemoveAvailabilityException_Should_Remove_Holiday_Block()
    {
        // Arrange
        var startUtc = DateTime.UtcNow.Date.AddDays(1);
        var endUtc = startUtc.AddDays(2);
        _doctor.AddBlackoutException(startUtc, endUtc, "National Holiday");
        var exId = _doctor.Exceptions.First().Id;

        var handler = new RemoveAvailabilityExceptionCommandHandler(_doctorRepo, _unitOfWork);
        var command = new RemoveAvailabilityExceptionCommand(_doctor.Id, exId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(_doctor.Exceptions);
    }
}
