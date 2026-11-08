using ClinicBooking.Domain.Entities;
using ClinicBooking.Infrastructure.Persistence;
using ClinicBooking.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicBooking.API.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = "ApiTestDb_" + Guid.NewGuid().ToString("N");

    public Guid DefaultTenantId { get; private set; }
    public string DefaultTenantSlug { get; private set; } = "apex-health";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove all existing EF Core and SQL Server service registrations
            var efDescriptors = services.Where(d =>
                d.ServiceType.FullName?.Contains("EntityFrameworkCore") == true ||
                d.ImplementationType?.FullName?.Contains("EntityFrameworkCore") == true ||
                d.ServiceType == typeof(DbContextOptions<ClinicBookingDbContext>) ||
                d.ServiceType == typeof(ClinicBookingDbContext) ||
                d.ServiceType == typeof(DbContextOptions)).ToList();

            foreach (var descriptor in efDescriptors)
            {
                services.Remove(descriptor);
            }

            // Register fresh isolated InMemory database for test runs
            services.AddDbContext<ClinicBookingDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
                options.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
            });

            // Build isolated provider and run seed data
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<ClinicBookingDbContext>();
            var seeder = scopedServices.GetRequiredService<DatabaseSeeder>();

            db.Database.EnsureCreated();
            seeder.SeedAsync().GetAwaiter().GetResult();

            var tenant = db.Tenants.IgnoreQueryFilters().First(t => t.Identifier == DefaultTenantSlug);
            DefaultTenantId = tenant.Id;
        });
    }
}
