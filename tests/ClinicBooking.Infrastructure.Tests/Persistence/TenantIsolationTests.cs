using ClinicBooking.Application.Contracts.Services;
using ClinicBooking.Domain.Entities;
using ClinicBooking.Domain.Enums;
using ClinicBooking.Infrastructure.Persistence;
using ClinicBooking.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace ClinicBooking.Infrastructure.Tests.Persistence;

public class TenantIsolationTests
{
    private readonly Guid _tenantA = Guid.NewGuid();
    private readonly Guid _tenantB = Guid.NewGuid();

    private ClinicBookingDbContext CreateContext(
        Guid? tenantId,
        bool isHost = false,
        Guid? currentUserId = null,
        string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<ClinicBookingDbContext>()
            .UseInMemoryDatabase(databaseName: dbName ?? ("TenantIsolationTestsDb_" + Guid.NewGuid().ToString("N")))
            .Options;

        var tenantContext = new TestTenantContext(tenantId, isHost);
        var currentUserService = new TestCurrentUserService(currentUserId);
        var dateTimeProvider = new SystemDateTimeProvider();

        return new ClinicBookingDbContext(options, tenantContext, dateTimeProvider, currentUserService);
    }

    [Fact]
    public async Task QueryFilter_TenantA_CannotSeeTenantBData()
    {
        var dbName = "SharedDb_" + Guid.NewGuid().ToString("N");

        // Seed via Host
        using (var hostContext = CreateContext(null, isHost: true, dbName: dbName))
        {
            var clinicA = Clinic.Create(_tenantA, "Clinic A", "123 St", "Cairo", "+201000000001");
            var clinicB = Clinic.Create(_tenantB, "Clinic B", "456 St", "Alexandria", "+201000000002");
            await hostContext.Clinics.AddRangeAsync(clinicA, clinicB);
            await hostContext.SaveChangesAsync();
        }

        // Act: Query as Tenant A
        using (var contextA = CreateContext(_tenantA, isHost: false, dbName: dbName))
        {
            var visibleClinics = await contextA.Clinics.ToListAsync();

            // Assert
            Assert.Single(visibleClinics);
            Assert.Equal("Clinic A", visibleClinics[0].Name);
        }
    }

    [Fact]
    public async Task QueryFilter_HostContext_SeesAllTenantsData()
    {
        var dbName = "SharedDb_" + Guid.NewGuid().ToString("N");

        using (var hostContext = CreateContext(null, isHost: true, dbName: dbName))
        {
            var clinicA = Clinic.Create(_tenantA, "Clinic A", "123 St", "Cairo", "+201000000001");
            var clinicB = Clinic.Create(_tenantB, "Clinic B", "456 St", "Alexandria", "+201000000002");
            await hostContext.Clinics.AddRangeAsync(clinicA, clinicB);
            await hostContext.SaveChangesAsync();

            var allClinics = await hostContext.Clinics.ToListAsync();
            Assert.Equal(2, allClinics.Count);
        }
    }

    [Fact]
    public async Task WriteIsolation_TenantACannotInsertTenantBEntity_ThrowsUnauthorizedAccessException()
    {
        // Arrange: Context scoped to Tenant A
        using var context = CreateContext(_tenantA);

        // Attempt to insert entity belonging to Tenant B
        var maliciousClinic = Clinic.Create(_tenantB, "Infiltrator Clinic", "Street", "City", "+201000000000");
        await context.Clinics.AddAsync(maliciousClinic);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task SaveChangesAsync_AutoPopulatesAuditFields()
    {
        // Arrange
        var userId = Guid.NewGuid();
        using var context = CreateContext(_tenantA, isHost: false, currentUserId: userId);

        var clinic = Clinic.Create(_tenantA, "Audit Clinic", "Address", "City", "+201000000099");
        await context.Clinics.AddAsync(clinic);

        // Act
        await context.SaveChangesAsync();

        // Assert
        Assert.NotEqual(default, clinic.CreatedAtUtc);
        Assert.Equal(userId.ToString(), clinic.CreatedBy);
    }

    private static string GetDbName(ClinicBookingDbContext context)
    {
        return context.Database.ProviderName ?? "InMemoryDb";
    }

    private sealed class TestTenantContext : ITenantContext
    {
        public Guid? TenantId { get; }
        public string? TenantIdentifier => null;
        public bool IsHost { get; }

        public TestTenantContext(Guid? tenantId, bool isHost)
        {
            TenantId = tenantId;
            IsHost = isHost;
        }
    }

    private sealed class TestCurrentUserService : ICurrentUserService
    {
        public Guid? UserId { get; }
        public string? Email => null;
        public string? Role => "Admin";
        public Guid? ClinicId => null;
        public bool IsAuthenticated => UserId.HasValue;

        public TestCurrentUserService(Guid? userId) => UserId = userId;
    }
}
