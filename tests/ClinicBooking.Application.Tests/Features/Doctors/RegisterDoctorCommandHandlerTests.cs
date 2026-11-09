using ClinicBooking.Application.Common.Localization;
using ClinicBooking.Application.Features.Doctors.Commands.RegisterDoctor;
using ClinicBooking.Application.Tests.TestHelpers;
using ClinicBooking.Domain.Entities;
using ClinicBooking.Domain.Enums;
using Xunit;

namespace ClinicBooking.Application.Tests.Features.Doctors;

public class RegisterDoctorCommandHandlerTests
{
    private readonly FakeUserRepository _userRepo = new();
    private readonly FakeDoctorRepository _doctorRepo = new();
    private readonly FakeTenantRepository _tenantRepo = new();
    private readonly FakeSpecialtyRepository _specialtyRepo = new();
    private readonly FakeClinicRepository _clinicRepo = new();
    private readonly FakePasswordHasher _passwordHasher = new();
    private readonly LocalizationService _localizationService = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private readonly RegisterDoctorCommandHandler _handler;
    private readonly Tenant _tenant;
    private readonly Specialty _specialty;
    private readonly Clinic _clinic;

    public RegisterDoctorCommandHandlerTests()
    {
        _tenant = Tenant.Create("Apex Network", "apex", 24, false);
        _tenantRepo.Seed(_tenant);

        _specialty = Specialty.Create("Cardiology", "Heart diseases");
        _specialtyRepo.Seed(_specialty);

        _clinic = Clinic.Create(_tenant.Id, "Apex Downtown", "10 Main St", "Cairo", "+201012345678");
        _clinicRepo.Seed(_clinic);

        _handler = new RegisterDoctorCommandHandler(
            _userRepo,
            _doctorRepo,
            _tenantRepo,
            _specialtyRepo,
            _clinicRepo,
            _passwordHasher,
            _localizationService,
            _unitOfWork);
    }

    [Fact]
    public async Task Handle_ValidRequest_RegistersUserDoctorAndClinicAssignment()
    {
        // Arrange
        var command = new RegisterDoctorCommand(
            TenantId: _tenant.Id,
            FirstName: "Ahmed",
            LastName: "Mahmoud",
            Email: "dr.ahmed@clinic.com",
            Password: "SecurePassword123!",
            SpecialtyId: _specialty.Id,
            LicenseNumber: "EGY-12345",
            BioEn: "Expert Cardiologist",
            BioAr: "استشاري أمراض القلب",
            ClinicId: _clinic.Id,
            ConsultationFee: 400,
            DefaultSlotDurationMinutes: 30,
            InitialShiftDay: DayOfWeek.Sunday,
            InitialShiftStartTime: new TimeOnly(9, 0),
            InitialShiftEndTime: new TimeOnly(17, 0));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Ahmed Mahmoud", result.Value.FullName);
        Assert.Equal("dr.ahmed@clinic.com", result.Value.Email);
        Assert.Equal("EGY-12345", result.Value.LicenseNumber);
        Assert.Equal("Cardiology", result.Value.SpecialtyName);
        Assert.Single(result.Value.DoctorClinics!);
        Assert.Equal("Apex Downtown", result.Value.DoctorClinics![0].ClinicName);
        Assert.Equal(400, result.Value.DoctorClinics[0].ConsultationFee);
        Assert.Single(result.Value.Availabilities);

        // Verify User was created
        var user = await _userRepo.GetByEmailAsync("dr.ahmed@clinic.com");
        Assert.NotNull(user);
        Assert.Equal(UserRole.Doctor, user!.Role);
        Assert.Equal(_clinic.Id, user.ClinicId);
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ReturnsConflictError()
    {
        // Arrange
        var existingUser = User.Create("existing@clinic.com", "Test", "User", UserRole.Doctor, _tenant.Id);
        await _userRepo.AddAsync(existingUser);

        var command = new RegisterDoctorCommand(
            TenantId: _tenant.Id,
            FirstName: "New",
            LastName: "Doctor",
            Email: "existing@clinic.com",
            Password: "SecurePassword123!",
            SpecialtyId: _specialty.Id,
            LicenseNumber: "EGY-99999",
            BioEn: "Bio");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("User.DuplicateEmail", result.Error.Code);
    }
}
