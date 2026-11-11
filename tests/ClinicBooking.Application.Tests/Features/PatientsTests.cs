using ClinicBooking.Application.Features.Patients.Commands.CreatePatient;
using ClinicBooking.Application.Features.Patients.Queries.GetPatients;
using ClinicBooking.Application.Tests.TestHelpers;
using ClinicBooking.Domain.Entities;
using ClinicBooking.Domain.Enums;
using FluentValidation.TestHelper;
using Xunit;

namespace ClinicBooking.Application.Tests.Features;

public class PatientsTests
{
    private readonly FakeUserRepository _userRepo = new();
    private readonly FakePatientRepository _patientRepo = new();
    private readonly FakeTenantRepository _tenantRepo = new();
    private readonly FakePasswordHasher _hasher = new();
    private readonly FakeUnitOfWork _uow = new();

    public PatientsTests()
    {
        var tenant = new Tenant(Guid.NewGuid(), "Main Medical Center", "main");
        _tenantRepo.Seed(tenant);
    }

    [Fact]
    public async Task CreatePatient_Should_Create_Patient_And_User_And_Return_Summary()
    {
        // Arrange
        var tenant = _tenantRepo.Tenants.First();
        var handler = new CreatePatientCommandHandler(_userRepo, _patientRepo, _tenantRepo, _hasher, _uow);
        var command = new CreatePatientCommand(
            tenant.Id,
            "Mohamed",
            "Ali",
            "mohamed.ali@example.com",
            "+201012345678",
            new DateOnly(1992, 4, 10),
            Gender.Male,
            "O+",
            "Penicillin",
            "Asthma",
            "+201098765432");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Mohamed Ali", result.Value.FullName);
        Assert.Equal("mohamed.ali@example.com", result.Value.Email);
        Assert.Equal("+201012345678", result.Value.PhoneNumber);
        Assert.Equal("O+", result.Value.BloodGroup);
        Assert.Equal("Penicillin", result.Value.Allergies);
        Assert.Equal("Asthma", result.Value.ChronicConditions);

        var createdPatient = await _patientRepo.GetByIdAsync(result.Value.Id);
        Assert.NotNull(createdPatient);
        Assert.Equal("O+", createdPatient.BloodGroup);

        var createdUser = await _userRepo.GetByIdAsync(createdPatient.UserId);
        Assert.NotNull(createdUser);
        Assert.Equal(UserRole.Patient, createdUser.Role);
    }

    [Fact]
    public void CreatePatientCommandValidator_Should_Fail_When_Required_Fields_Missing()
    {
        // Arrange
        var validator = new CreatePatientCommandValidator();
        var command = new CreatePatientCommand(
            Guid.NewGuid(),
            "",
            "",
            "invalid-email",
            null,
            default,
            Gender.Male);

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FirstName);
        result.ShouldHaveValidationErrorFor(x => x.LastName);
        result.ShouldHaveValidationErrorFor(x => x.Email);
        result.ShouldHaveValidationErrorFor(x => x.DateOfBirth);
    }

    [Fact]
    public async Task GetPatients_Should_Return_List_Of_PatientSummaryDtos()
    {
        // Arrange
        var tenant = _tenantRepo.Tenants.First();
        var user = User.Create("sarah.patient@test.com", "Sarah", "Samir", UserRole.Patient, tenant.Id);
        _userRepo.Seed(user);

        var patient = Patient.Create(tenant.Id, user.Id, new DateOnly(1996, 7, 20), Gender.Female, "+201055556666");
        patient.UpdateMedicalHistory("A+", "Peanuts", null, null);
        _patientRepo.Seed(patient);

        var handler = new GetPatientsQueryHandler(_patientRepo, _userRepo);
        var query = new GetPatientsQuery(tenant.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value);
        var pDto = result.Value.First(p => p.Id == patient.Id);
        Assert.Equal("Sarah Samir", pDto.FullName);
        Assert.Equal("sarah.patient@test.com", pDto.Email);
        Assert.Equal("A+", pDto.BloodGroup);
        Assert.Equal("Female", pDto.Gender);
    }
}
