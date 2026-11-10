using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Contracts.Services;
using ClinicBooking.Infrastructure.Authentication;
using ClinicBooking.Infrastructure.Persistence;
using ClinicBooking.Infrastructure.Persistence.Repositories;
using ClinicBooking.Infrastructure.Persistence.Seed;
using ClinicBooking.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicBooking.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 1. JWT Configuration with Options Validation
        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection(JwtSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // 2. Database Context with SQL Server (only if not already registered by testing harness)
        if (!services.Any(d => d.ServiceType == typeof(DbContextOptions<ClinicBookingDbContext>)))
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? configuration.GetConnectionString("SqlServer")
                ?? "Server=(localdb)\\mssqllocaldb;Database=ClinicBookingDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

            services.AddDbContext<ClinicBookingDbContext>(options =>
            {
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly(typeof(ClinicBookingDbContext).Assembly.FullName);
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);
                });
            });
        }

        // 3. Unit of Work & Repositories
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISpecialtyRepository, SpecialtyRepository>();
        services.AddScoped<IClinicRepository, ClinicRepository>();
        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IDoctorRepository, DoctorRepository>();
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<IConsultationRepository, ConsultationRepository>();
        services.AddScoped<IPatientVitalSignsRepository, PatientVitalSignsRepository>();
        services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IMedicalAttachmentRepository, MedicalAttachmentRepository>();
        services.AddScoped<IMedicalInventoryRepository, MedicalInventoryRepository>();
        services.AddScoped<IInsuranceProviderRepository, InsuranceProviderRepository>();

        // 4. Infrastructure Services
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITokenService, TokenService>();

        // 5. Database Seeder
        services.AddScoped<DatabaseSeeder>();

        return services;
    }
}
