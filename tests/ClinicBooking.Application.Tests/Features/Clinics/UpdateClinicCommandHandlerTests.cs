using ClinicBooking.Application.Common.Localization;
using ClinicBooking.Application.Features.Clinics.Commands.UpdateClinic;
using ClinicBooking.Application.Tests.TestHelpers;
using ClinicBooking.Domain.Entities;
using Xunit;

namespace ClinicBooking.Application.Tests.Features.Clinics;

public class UpdateClinicCommandHandlerTests
{
    private readonly FakeClinicRepository _clinicRepo = new();
    private readonly LocalizationService _localizationService = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly UpdateClinicCommandHandler _handler;

    public UpdateClinicCommandHandlerTests()
    {
        _handler = new UpdateClinicCommandHandler(_clinicRepo, _localizationService, _unitOfWork);
    }

    [Fact]
    public async Task Handle_ExistingClinic_UpdatesDetailsSuccessfully()
    {
        // Arrange
        var clinic = Clinic.Create(Guid.NewGuid(), "Old Branch", "1 Old St", "Cairo", "+201011112222");
        _clinicRepo.Seed(clinic);

        var command = new UpdateClinicCommand(
            Id: clinic.Id,
            Name: "Apex Heliopolis Branch",
            Address: "50 Heliopolis Square",
            City: "Cairo",
            PhoneNumber: "+201099998888",
            IsActive: true,
            NameAr: "فرع مصر الجديدة",
            AddressAr: "٥٠ ميدان مصر الجديدة",
            CityAr: "القاهرة");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Apex Heliopolis Branch", result.Value.Name);
        Assert.Equal("50 Heliopolis Square", result.Value.Address);
        Assert.Equal("+201099998888", result.Value.PhoneNumber);

        var updated = await _clinicRepo.GetByIdAsync(clinic.Id);
        Assert.NotNull(updated);
        Assert.Equal("Apex Heliopolis Branch", updated!.NameEn);
        Assert.Equal("فرع مصر الجديدة", updated.NameAr);
    }

    [Fact]
    public async Task Handle_NonExistentClinic_ReturnsNotFoundError()
    {
        // Arrange
        var command = new UpdateClinicCommand(
            Id: Guid.NewGuid(),
            Name: "Non Existent",
            Address: "Address",
            City: "City",
            PhoneNumber: "+201012345678");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Clinic.NotFound", result.Error.Code);
    }
}
