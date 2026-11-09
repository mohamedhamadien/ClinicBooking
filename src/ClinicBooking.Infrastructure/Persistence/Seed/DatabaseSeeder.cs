using ClinicBooking.Application.Contracts.Services;
using ClinicBooking.Domain.Entities;
using ClinicBooking.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClinicBooking.Infrastructure.Persistence.Seed;

public sealed class DatabaseSeeder
{
    private readonly ClinicBookingDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        ClinicBookingDbContext dbContext,
        IPasswordHasher passwordHasher,
        ILogger<DatabaseSeeder> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting database seeding...");

        // 1. Seed Default Tenant
        var defaultTenant = await _dbContext.Tenants
            .FirstOrDefaultAsync(t => t.Identifier == "apex-health", cancellationToken);

        if (defaultTenant is null)
        {
            defaultTenant = Tenant.Create(
                nameEn: "Apex Healthcare Network",
                identifier: "apex-health",
                cancellationNoticeHours: 24,
                requiresDoctorApproval: false,
                nameAr: "شبكة أبيكس للرعاية الصحية");

            await _dbContext.Tenants.AddAsync(defaultTenant, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seeded default tenant: {Identifier}", defaultTenant.Identifier);
        }

        // 2. Seed System Administrator
        var adminEmail = "admin@clinicbooking.com";
        var adminEmailVo = Domain.ValueObjects.EmailAddress.Create(adminEmail);
        var adminExists = await _dbContext.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == adminEmailVo, cancellationToken);

        if (!adminExists)
        {
            var adminUser = User.Create(
                adminEmail,
                firstName: "System",
                lastName: "Administrator",
                role: UserRole.Admin,
                tenantId: defaultTenant.Id);

            adminUser.SetPasswordHash(_passwordHasher.HashPassword("Admin@Secure123!"));
            await _dbContext.Users.AddAsync(adminUser, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seeded system administrator: {Email}", adminEmail);
        }

        // 3. Seed Global Medical Specialties (Bilingual)
        var existingSpecialties = await _dbContext.Specialties
            .IgnoreQueryFilters()
            .Select(s => s.NameEn)
            .ToListAsync(cancellationToken);

        var defaultSpecialties = new List<(string NameEn, string NameAr, string DescEn, string DescAr)>
        {
            ("Cardiology", "أمراض القلب",
             "Comprehensive heart and cardiovascular system diagnostic and therapeutic care.",
             "تشخيص وعلاج أمراض القلب والأوعية الدموية والشرايين."),

            ("Dermatology", "الأمراض الجلدية",
             "Diagnosis and treatment of skin, hair, and nail conditions.",
             "علاج وتشخيص جميع أمراض الجلد والشعر والأظافر."),

            ("Pediatrics", "طب الأطفال",
             "Medical care for infants, children, and adolescents.",
             "الرعاية الطبية المتكاملة للأطفال والرضع وحديثي الولادة."),

            ("Orthopedics", "جراحة العظام",
             "Care for bones, joints, ligaments, tendons, and muscles.",
             "علاج وتشخيص إصابات وجراحات العظام والمفاصل والعمود الفقري."),

            ("Neurology", "المخ والأعصاب",
             "Disorders of the brain, spinal cord, and nervous system.",
             "تشخيص وعلاج اضطرابات المخ والأعصاب والعمود الفقري."),

            ("Ophthalmology", "طب العيون",
             "Eye examinations, vision correction, and surgical procedures.",
             "فحوصات النظر وعلاج وجراحات العيون والشبكية."),

            ("Internal Medicine", "الباطنة العامة",
             "Prevention, diagnosis, and treatment of adult internal diseases.",
             "الوقاية والتشخيص والعلاج للأمراض الباطنية والمزمنة لدى البالغين."),

            ("Dentistry", "طب وجراحة الأسنان",
             "Comprehensive oral health care, restorations, and dental surgery.",
             "علاج وجراحة وتجميل الأسنان واللثة وزراعة الأسنان.")
        };

        foreach (var (nameEn, nameAr, descEn, descAr) in defaultSpecialties)
        {
            if (!existingSpecialties.Contains(nameEn, StringComparer.OrdinalIgnoreCase))
            {
                var specialty = Specialty.Create(
                    nameEn,
                    descEn,
                    tenantId: null, // Global specialty available to all tenants
                    nameAr: nameAr,
                    descriptionAr: descAr);

                await _dbContext.Specialties.AddAsync(specialty, cancellationToken);
            }
        }

        // 4. Seed Default Clinic
        var clinicExists = await _dbContext.Clinics
            .IgnoreQueryFilters()
            .AnyAsync(c => c.TenantId == defaultTenant.Id, cancellationToken);

        if (!clinicExists)
        {
            var defaultClinic = Clinic.Create(
                tenantId: defaultTenant.Id,
                nameEn: "Apex Central Clinic",
                addressEn: "100 Medical Center Blvd",
                cityEn: "Cairo",
                phoneNumber: "+201012345678",
                nameAr: "عيادة أبيكس المركزية",
                addressAr: "١٠٠ شارع المركز الطبي",
                cityAr: "القاهرة");

            await _dbContext.Clinics.AddAsync(defaultClinic, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        // 5. Seed Clinic Administrator
        var clinicAdminEmail = "clinicadmin@clinicbooking.com";
        var clinicAdminEmailVo = Domain.ValueObjects.EmailAddress.Create(clinicAdminEmail);
        var clinicAdminExists = await _dbContext.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == clinicAdminEmailVo, cancellationToken);

        if (!clinicAdminExists)
        {
            var defaultClinicEntity = await _dbContext.Clinics
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.TenantId == defaultTenant.Id, cancellationToken);

            var clinicAdminUser = User.Create(
                clinicAdminEmail,
                firstName: "Clinic",
                lastName: "Administrator",
                role: UserRole.ClinicAdmin,
                tenantId: defaultTenant.Id,
                clinicId: defaultClinicEntity?.Id);

            clinicAdminUser.SetPasswordHash(_passwordHasher.HashPassword("ClinicAdmin@Secure123!"));
            await _dbContext.Users.AddAsync(clinicAdminUser, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Database seeding completed successfully.");
    }
}
