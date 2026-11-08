using ClinicBooking.Application.Contracts.Services;
using ClinicBooking.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ClinicBooking.Infrastructure.Persistence;

public sealed class ClinicBookingDbContextFactory : IDesignTimeDbContextFactory<ClinicBookingDbContext>
{
    public ClinicBookingDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ClinicBookingDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=localhost;Database=ClinicBookingDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True",
            sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(ClinicBookingDbContext).Assembly.FullName);
            });

        return new ClinicBookingDbContext(
            optionsBuilder.Options,
            new DesignTimeTenantContext(),
            new SystemDateTimeProvider(),
            currentUserService: null);
    }

    private sealed class DesignTimeTenantContext : ITenantContext
    {
        public Guid? TenantId => null;
        public string? TenantIdentifier => null;
        public bool IsHost => true; // Design-time runs as host to configure all tables and schemas
    }
}
