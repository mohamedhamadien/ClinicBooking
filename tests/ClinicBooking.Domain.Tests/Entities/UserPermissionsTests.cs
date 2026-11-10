using ClinicBooking.Domain.Constants;
using ClinicBooking.Domain.Entities;
using ClinicBooking.Domain.Enums;
using Xunit;

namespace ClinicBooking.Domain.Tests.Entities;

public class UserPermissionsTests
{
    [Fact]
    public void Admin_Should_Have_All_Permissions()
    {
        var admin = User.Create("admin@test.com", "Admin", "User", UserRole.Admin);

        var effective = admin.GetEffectivePermissions();

        Assert.Equal(ClinicPermissions.All.Count, effective.Count);
        Assert.True(admin.HasPermission(ClinicPermissions.ScreenOverview));
        Assert.True(admin.HasPermission(ClinicPermissions.StaffManage));
        Assert.True(admin.HasPermission(ClinicPermissions.BillingCollectPayment));
    }

    [Fact]
    public void Receptionist_Default_Preset_Contains_Queue_And_Billing_Permissions()
    {
        var receptionist = User.Create("reception@test.com", "Reception", "Staff", UserRole.Receptionist);

        Assert.True(receptionist.HasPermission(ClinicPermissions.ScreenQueue));
        Assert.True(receptionist.HasPermission(ClinicPermissions.QueueCheckIn));
        Assert.True(receptionist.HasPermission(ClinicPermissions.ScreenBilling));
        Assert.True(receptionist.HasPermission(ClinicPermissions.BillingCollectPayment));
        // Should not have doctor consultation permissions
        Assert.False(receptionist.HasPermission(ClinicPermissions.ConsultationCreate));
    }

    [Fact]
    public void Nurse_Default_Preset_Contains_Queue_And_Vitals_Permissions()
    {
        var nurse = User.Create("nurse@test.com", "Nurse", "Staff", UserRole.Nurse);

        Assert.True(nurse.HasPermission(ClinicPermissions.ScreenQueue));
        Assert.True(nurse.HasPermission(ClinicPermissions.QueueCallPatient));
        Assert.True(nurse.HasPermission(ClinicPermissions.MedicalRecordsUpdate));
        Assert.False(nurse.HasPermission(ClinicPermissions.StaffManage));
    }

    [Fact]
    public void Custom_Explicit_Permissions_Override_Preset()
    {
        var staff = User.Create("custom@test.com", "Custom", "Staff", UserRole.Staff);

        staff.SetPermissions([ClinicPermissions.ScreenBilling, ClinicPermissions.BillingCreateInvoice]);

        Assert.True(staff.HasPermission(ClinicPermissions.ScreenBilling));
        Assert.True(staff.HasPermission(ClinicPermissions.BillingCreateInvoice));
        Assert.False(staff.HasPermission(ClinicPermissions.ScreenDoctors));
        Assert.False(staff.HasPermission(ClinicPermissions.StaffManage));
    }

    [Fact]
    public void Deactivate_And_Activate_Staff_Works()
    {
        var staff = User.Create("staff@test.com", "Sam", "Smith", UserRole.Staff);
        Assert.True(staff.IsActive);

        staff.Deactivate();
        Assert.False(staff.IsActive);

        staff.Activate();
        Assert.True(staff.IsActive);
    }
}
