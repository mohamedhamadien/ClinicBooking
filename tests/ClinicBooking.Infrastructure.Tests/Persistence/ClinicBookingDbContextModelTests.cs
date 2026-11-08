using ClinicBooking.Application.Contracts.Services;
using ClinicBooking.Domain.Entities;
using ClinicBooking.Infrastructure.Persistence;
using ClinicBooking.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace ClinicBooking.Infrastructure.Tests.Persistence;

public class ClinicBookingDbContextModelTests
{
    private readonly ClinicBookingDbContext _dbContext;

    public ClinicBookingDbContextModelTests()
    {
        var options = new DbContextOptionsBuilder<ClinicBookingDbContext>()
            .UseSqlServer("Server=localhost;Database=Dummy;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        _dbContext = new ClinicBookingDbContext(
            options,
            new MockTenantContext(Guid.NewGuid(), "test-tenant", false),
            new SystemDateTimeProvider());
    }

    [Fact]
    public void Model_ContainsConfiguredEntitiesAndTables()
    {
        var model = _dbContext.Model;

        Assert.NotNull(model.FindEntityType(typeof(Tenant)));
        Assert.NotNull(model.FindEntityType(typeof(User)));
        Assert.NotNull(model.FindEntityType(typeof(RefreshToken)));
        Assert.NotNull(model.FindEntityType(typeof(Specialty)));
        Assert.NotNull(model.FindEntityType(typeof(Clinic)));
        Assert.NotNull(model.FindEntityType(typeof(Doctor)));
        Assert.NotNull(model.FindEntityType(typeof(Patient)));
        Assert.NotNull(model.FindEntityType(typeof(Appointment)));
    }

    [Fact]
    public void Appointment_HasFilteredUniqueDoubleBookingIndexes()
    {
        var entityType = _dbContext.Model.FindEntityType(typeof(Appointment));
        Assert.NotNull(entityType);

        var doctorSlotIndex = entityType.GetIndexes()
            .FirstOrDefault(i => i.GetDatabaseName() == AppointmentIndexNames.DoctorSlotUnique);

        Assert.NotNull(doctorSlotIndex);
        Assert.True(doctorSlotIndex.IsUnique);
        Assert.Equal(AppointmentIndexNames.ActiveSlotFilter, doctorSlotIndex.GetFilter());

        var patientSlotIndex = entityType.GetIndexes()
            .FirstOrDefault(i => i.GetDatabaseName() == AppointmentIndexNames.PatientSlotUnique);

        Assert.NotNull(patientSlotIndex);
        Assert.True(patientSlotIndex.IsUnique);
        Assert.Equal(AppointmentIndexNames.ActiveSlotFilter, patientSlotIndex.GetFilter());
    }

    [Fact]
    public void Appointment_HasCheckConstraint_EndAfterStart()
    {
        var designModel = _dbContext.GetService<IDesignTimeModel>().Model;
        var entityType = designModel.FindEntityType(typeof(Appointment));
        Assert.NotNull(entityType);

        var constraint = entityType.GetCheckConstraints()
            .FirstOrDefault(c => c.Name == "CK_Appointments_EndAfterStart");

        Assert.NotNull(constraint);
        Assert.Equal("[EndTimeUtc] > [StartTimeUtc]", constraint.Sql);
    }

    private sealed class MockTenantContext : ITenantContext
    {
        public Guid? TenantId { get; }
        public string? TenantIdentifier { get; }
        public bool IsHost { get; }

        public MockTenantContext(Guid? tenantId, string? identifier, bool isHost)
        {
            TenantId = tenantId;
            TenantIdentifier = identifier;
            IsHost = isHost;
        }
    }
}
