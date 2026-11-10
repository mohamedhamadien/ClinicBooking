using ClinicBooking.Application.Features.Staff.Commands.CreateStaffMember;
using ClinicBooking.Application.Features.Staff.Commands.ToggleStaffStatus;
using ClinicBooking.Application.Features.Staff.Commands.UpdateStaffPermissions;
using ClinicBooking.Application.Features.Staff.Queries.GetPermissionCatalog;
using ClinicBooking.Application.Features.Staff.Queries.GetStaffMembers;
using ClinicBooking.Application.Tests.TestHelpers;
using ClinicBooking.Domain.Constants;
using ClinicBooking.Domain.Entities;
using ClinicBooking.Domain.Enums;
using Xunit;

namespace ClinicBooking.Application.Tests.Features.Staff;

public class StaffFeaturesTests
{
    private readonly FakeUserRepository _userRepo = new();
    private readonly FakeClinicRepository _clinicRepo = new();
    private readonly FakePasswordHasher _hasher = new();
    private readonly FakeUnitOfWork _uow = new();

    [Fact]
    public async Task GetPermissionCatalog_Returns_All_Items_And_Presets()
    {
        var handler = new GetPermissionCatalogQueryHandler();
        var result = await handler.Handle(new GetPermissionCatalogQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value.Permissions);
        Assert.NotEmpty(result.Value.RolePresets);
        Assert.Contains(result.Value.Permissions, p => p.Key == ClinicPermissions.ScreenQueue);
    }

    [Fact]
    public async Task CreateStaffMember_Successfully_Creates_Staff_With_Role_And_Permissions()
    {
        var handler = new CreateStaffMemberCommandHandler(_userRepo, _clinicRepo, _hasher, _uow);
        var command = new CreateStaffMemberCommand(
            TenantId: Guid.NewGuid(),
            Email: "receptionist1@clinic.com",
            Password: "Password123!",
            FirstName: "Sarah",
            LastName: "Connor",
            Role: "Receptionist",
            ClinicId: null,
            Permissions: [ClinicPermissions.ScreenQueue, ClinicPermissions.QueueCheckIn]);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Sarah Connor", result.Value.FullName);
        Assert.Equal("Receptionist", result.Value.Role);
        Assert.Contains(ClinicPermissions.QueueCheckIn, result.Value.Permissions);
    }

    [Fact]
    public async Task UpdateStaffPermissions_Modifies_Role_And_Granted_Permissions()
    {
        var staff = User.Create("nurse@clinic.com", "Jane", "Doe", UserRole.Nurse);
        await _userRepo.AddAsync(staff);

        var handler = new UpdateStaffPermissionsCommandHandler(_userRepo, _clinicRepo, _uow);
        var command = new UpdateStaffPermissionsCommand(
            staff.Id,
            Role: "Staff",
            ClinicId: null,
            Permissions: [ClinicPermissions.ScreenOverview, ClinicPermissions.ScreenQueue]);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Staff", result.Value.Role);
        Assert.Contains(ClinicPermissions.ScreenOverview, result.Value.Permissions);
        Assert.Contains(ClinicPermissions.ScreenQueue, result.Value.Permissions);
    }

    [Fact]
    public async Task ToggleStaffStatus_Deactivates_And_Reactivates_Staff()
    {
        var staff = User.Create("active@clinic.com", "John", "Doe", UserRole.Staff);
        await _userRepo.AddAsync(staff);

        var handler = new ToggleStaffStatusCommandHandler(_userRepo, _clinicRepo, _uow);
        
        var result1 = await handler.Handle(new ToggleStaffStatusCommand(staff.Id), CancellationToken.None);
        Assert.True(result1.IsSuccess);
        Assert.False(result1.Value.IsActive);

        var result2 = await handler.Handle(new ToggleStaffStatusCommand(staff.Id), CancellationToken.None);
        Assert.True(result2.IsSuccess);
        Assert.True(result2.Value.IsActive);
    }
}
