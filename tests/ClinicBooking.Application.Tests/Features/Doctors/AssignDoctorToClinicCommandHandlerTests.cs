using ClinicBooking.Application.Features.Doctors.Commands.AssignDoctorToClinic;
using ClinicBooking.Application.Tests.TestHelpers;
using ClinicBooking.Domain.Entities;
using Xunit;

namespace ClinicBooking.Application.Tests.Features.Doctors;

public class AssignDoctorToClinicCommandHandlerTests
{
    private readonly FakeDoctorRepository _doctorRepo = new();
    private readonly FakeClinicRepository _clinicRepo = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly AssignDoctorToClinicCommandHandler _handler;

    public AssignDoctorToClinicCommandHandlerTests()
    {
        _handler = new AssignDoctorToClinicCommandHandler(_doctorRepo, _clinicRepo, _unitOfWork);
    }

    [Fact]
    public async Task Handle_ValidDoctorAndClinic_AssignsSuccessfully()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var doctor = Doctor.Create(tenantId, Guid.NewGuid(), Guid.NewGuid(), "DOC-1", "Bio");
        var clinic = Clinic.Create(tenantId, "Branch", "Address", "City", "+201012345678");

        _doctorRepo.Seed(doctor);
        _clinicRepo.Seed(clinic);

        var command = new AssignDoctorToClinicCommand(doctor.Id, clinic.Id, 500, true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(doctor.DoctorClinics);
        Assert.Equal(clinic.Id, doctor.DoctorClinics.First().ClinicId);
        Assert.Equal(500, doctor.DoctorClinics.First().ConsultationFee);
    }
}
