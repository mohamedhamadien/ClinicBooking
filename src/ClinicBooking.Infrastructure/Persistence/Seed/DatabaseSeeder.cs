using ClinicBooking.Application.Contracts.Services;
using ClinicBooking.Domain.Constants;
using ClinicBooking.Domain.Entities;
using ClinicBooking.Domain.Enums;
using ClinicBooking.Domain.ValueObjects;
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
        _logger.LogInformation("Starting database seeding with complete clinic ecosystem data...");

        // =========================================================================
        // 1. TENANT
        // =========================================================================
        var defaultTenant = await _dbContext.Tenants
            .FirstOrDefaultAsync(t => t.Identifier == "apex-health", cancellationToken);

        if (defaultTenant is null)
        {
            defaultTenant = Tenant.Create(
                nameEn: "Apex Healthcare Network",
                identifier: "apex-health",
                cancellationNoticeHours: 24,
                requiresDoctorApproval: false,
                nameAr: "شبكة أبيكس للرعاية الصحية المتكاملة");

            await _dbContext.Tenants.AddAsync(defaultTenant, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seeded default tenant: {Identifier}", defaultTenant.Identifier);
        }

        // =========================================================================
        // 2. CLINIC BRANCHES
        // =========================================================================
        var clinics = await _dbContext.Clinics
            .IgnoreQueryFilters()
            .Where(c => c.TenantId == defaultTenant.Id)
            .ToListAsync(cancellationToken);

        Clinic centralClinic;
        Clinic nileClinic;

        if (clinics.Count == 0)
        {
            centralClinic = Clinic.Create(
                tenantId: defaultTenant.Id,
                nameEn: "Apex Central Medical Center",
                addressEn: "100 Medical Center Blvd, Downtown",
                cityEn: "Cairo",
                phoneNumber: "+201012345678",
                nameAr: "مركز أبيكس الطبي المركزي",
                addressAr: "١٠٠ شارع المركز الطبي، وسط البلد",
                cityAr: "القاهرة");

            nileClinic = Clinic.Create(
                tenantId: defaultTenant.Id,
                nameEn: "Apex Nile Specialized Clinic",
                addressEn: "45 Nile Corniche Plaza",
                cityEn: "Giza",
                phoneNumber: "+201098765432",
                nameAr: "عيادة أبيكس التخصصية بكورنيش النيل",
                addressAr: "٤٥ كورنيش النيل، الدقي",
                cityAr: "الجيزة");

            await _dbContext.Clinics.AddRangeAsync(new[] { centralClinic, nileClinic }, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        else
        {
            centralClinic = clinics[0];
            nileClinic = clinics.Count > 1 ? clinics[1] : clinics[0];
        }

        // =========================================================================
        // 3. MEDICAL SPECIALTIES (Bilingual)
        // =========================================================================
        var existingSpecialties = await _dbContext.Specialties
            .IgnoreQueryFilters()
            .ToListAsync(cancellationToken);

        var specialtyDefs = new (string En, string Ar, string DescEn, string DescAr)[]
        {
            ("Cardiology", "أمراض القلب", "Comprehensive heart and cardiovascular system diagnostic and therapeutic care.", "تشخيص وعلاج أمراض القلب والأوعية الدموية والشرايين."),
            ("Pediatrics", "طب الأطفال ورعاية حديثي الولادة", "Comprehensive medical care for infants, children, and adolescents.", "الرعاية الطبية المتكاملة للأطفال والرضع وحديثي الولادة."),
            ("Dermatology", "الأمراض الجلدية والتجميل والليزر", "Expert medical and aesthetic care for skin, hair, and nail conditions.", "تشخيص وعلاج جميع أمراض الجلد والشعر والأظافر والتجميل."),
            ("Orthopedics", "جراحة العظام والمفاصل والعمود الفقري", "Surgical and conservative treatment of musculoskeletal disorders.", "علاج وجراحات العظام والمفاصل والكسور والعمود الفقري."),
            ("Neurology", "المخ والأعصاب والاضطرابات العصبية", "Care for central and peripheral nervous system disorders.", "تشخيص وعلاج اضطرابات المخ والأعصاب والسكتات والصداع."),
            ("Internal Medicine", "الباطنة العامة والأمراض المزمنة", "Prevention, diagnosis, and management of adult chronic internal conditions.", "الوقاية والتشخيص والعلاج للأمراض الباطنية والسكر والضغط."),
            ("Dentistry", "طب وجراحة وتجميل الأسنان", "Oral healthcare, endodontics, prosthodontics, and aesthetic dental surgery.", "علاج وجراحة وتجميل الأسنان وزراعة الأسنان وتقويم الفكين.")
        };

        var specialtyMap = new Dictionary<string, Specialty>(StringComparer.OrdinalIgnoreCase);

        foreach (var def in specialtyDefs)
        {
            var match = existingSpecialties.FirstOrDefault(s => s.NameEn.Equals(def.En, StringComparison.OrdinalIgnoreCase));
            if (match is null)
            {
                match = Specialty.Create(def.En, def.DescEn, null, def.Ar, def.DescAr);
                await _dbContext.Specialties.AddAsync(match, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            specialtyMap[def.En] = match;
        }

        // =========================================================================
        // 4. USERS & STAFF ROLES
        // =========================================================================
        async Task<User> EnsureUserAsync(
            string email,
            string firstName,
            string lastName,
            UserRole role,
            string password,
            Guid? clinicId = null)
        {
            var emailVo = EmailAddress.Create(email);
            var existing = await _dbContext.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Email == emailVo, cancellationToken);

            if (existing is not null) return existing;

            var user = User.Create(email, firstName, lastName, role, defaultTenant.Id, clinicId);
            user.SetPasswordHash(_passwordHasher.HashPassword(password));
            await _dbContext.Users.AddAsync(user, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return user;
        }

        // System Admin
        var adminUser = await EnsureUserAsync("admin@clinicbooking.com", "System", "Administrator", UserRole.Admin, "Admin@Secure123!");

        // Clinic Admin
        var clinicAdminUser = await EnsureUserAsync("clinicadmin@clinicbooking.com", "Clinic", "Director", UserRole.ClinicAdmin, "ClinicAdmin@Secure123!", centralClinic.Id);

        // Receptionist
        var receptionistUser = await EnsureUserAsync("receptionist@clinicbooking.com", "Noha", "Fahmy", UserRole.Receptionist, "Staff@Secure123!", centralClinic.Id);

        // Nurse
        var nurseUser = await EnsureUserAsync("nurse@clinicbooking.com", "Mariam", "Soliman", UserRole.Nurse, "Staff@Secure123!", centralClinic.Id);

        // Accountant
        var accountantUser = await EnsureUserAsync("accountant@clinicbooking.com", "Hossam", "Adel", UserRole.Accountant, "Staff@Secure123!", centralClinic.Id);

        // Doctors Users
        var doc1User = await EnsureUserAsync("dr.ahmad@clinicbooking.com", "Ahmad", "Hassan", UserRole.Doctor, "Doctor@Secure123!", centralClinic.Id);
        var doc2User = await EnsureUserAsync("dr.mona@clinicbooking.com", "Mona", "El-Sayed", UserRole.Doctor, "Doctor@Secure123!", centralClinic.Id);
        var doc3User = await EnsureUserAsync("dr.tarek@clinicbooking.com", "Tarek", "Mansour", UserRole.Doctor, "Doctor@Secure123!", nileClinic.Id);

        // Patient Users
        var pat1User = await EnsureUserAsync("patient.sarah@example.com", "Sarah", "Ali", UserRole.Patient, "Patient@Secure123!");
        var pat2User = await EnsureUserAsync("patient.omar@example.com", "Omar", "Khaled", UserRole.Patient, "Patient@Secure123!");
        var pat3User = await EnsureUserAsync("patient.layla@example.com", "Layla", "Mahmoud", UserRole.Patient, "Patient@Secure123!");
        var pat4User = await EnsureUserAsync("patient.youssef@example.com", "Youssef", "Ibrahim", UserRole.Patient, "Patient@Secure123!");

        // Seed Staff Permissions Presets
        async Task SeedPermissionsAsync(User user, string roleName)
        {
            if (ClinicPermissions.RolePresets.TryGetValue(roleName, out var permissions))
            {
                var existingKeys = await _dbContext.UserPermissions
                    .IgnoreQueryFilters()
                    .Where(p => p.UserId == user.Id)
                    .Select(p => p.Permission)
                    .ToListAsync(cancellationToken);

                foreach (var perm in permissions)
                {
                    if (!existingKeys.Contains(perm))
                    {
                        var up = new UserPermission(Guid.NewGuid(), user.Id, perm, defaultTenant.Id, DateTime.UtcNow);
                        await _dbContext.UserPermissions.AddAsync(up, cancellationToken);
                    }
                }
            }
        }

        await SeedPermissionsAsync(receptionistUser, "Receptionist");
        await SeedPermissionsAsync(nurseUser, "Nurse");
        await SeedPermissionsAsync(accountantUser, "Accountant");
        await SeedPermissionsAsync(doc1User, "Doctor");
        await SeedPermissionsAsync(doc2User, "Doctor");
        await SeedPermissionsAsync(doc3User, "Doctor");
        await _dbContext.SaveChangesAsync(cancellationToken);

        // =========================================================================
        // 5. DOCTOR ENTITIES & AVAILABILITY SCHEDULES
        // =========================================================================
        async Task<Doctor> EnsureDoctorAsync(
            User user,
            Specialty specialty,
            string license,
            string bio,
            Guid clinicId,
            decimal feeAmount)
        {
            var existing = await _dbContext.Doctors
                .IgnoreQueryFilters()
                .Include(d => d.Availabilities)
                .FirstOrDefaultAsync(d => d.UserId == user.Id, cancellationToken);

            if (existing is not null) return existing;

            var doctor = Doctor.Create(
                defaultTenant.Id,
                user.Id,
                specialty.Id,
                license,
                bio,
                defaultSlotDurationMinutes: 30);

            // Add Availabilities across multiple days
            doctor.AddAvailability(clinicId, DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(15, 0));
            doctor.AddAvailability(clinicId, DayOfWeek.Wednesday, new TimeOnly(9, 0), new TimeOnly(15, 0));
            doctor.AddAvailability(clinicId, DayOfWeek.Saturday, new TimeOnly(10, 0), new TimeOnly(16, 0));

            await _dbContext.Doctors.AddAsync(doctor, cancellationToken);

            var doctorClinic = new DoctorClinic(Guid.NewGuid(), defaultTenant.Id, doctor.Id, clinicId, feeAmount, true);
            await _dbContext.DoctorClinics.AddAsync(doctorClinic, cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);
            return doctor;
        }

        var drAhmad = await EnsureDoctorAsync(
            doc1User,
            specialtyMap["Cardiology"],
            "LIC-CARD-001",
            "Senior Consultant Cardiologist, Fellow of European Society of Cardiology.",
            centralClinic.Id,
            500m);

        var drMona = await EnsureDoctorAsync(
            doc2User,
            specialtyMap["Pediatrics"],
            "LIC-PED-002",
            "Consultant Pediatrician and Neonatologist, Member of Royal College of Pediatrics.",
            centralClinic.Id,
            400m);

        var drTarek = await EnsureDoctorAsync(
            doc3User,
            specialtyMap["Dermatology"],
            "LIC-DERM-003",
            "Specialist Dermatologist and Laser Surgeon, Certified by Arab Board.",
            nileClinic.Id,
            450m);

        // =========================================================================
        // 6. PATIENT ENTITIES & CLINICAL HISTORIES
        // =========================================================================
        async Task<Patient> EnsurePatientAsync(
            User user,
            DateOnly dob,
            Gender gender,
            string phone,
            string bloodGroup,
            string allergies,
            string chronicConditions)
        {
            var existing = await _dbContext.Patients
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.UserId == user.Id, cancellationToken);

            if (existing is not null) return existing;

            var patient = Patient.Create(
                defaultTenant.Id,
                user.Id,
                dob,
                gender,
                phone);

            patient.UpdateMedicalHistory(
                bloodGroup,
                allergies,
                chronicConditions,
                "Regular clinic follow-up patient with verified electronic records.");

            await _dbContext.Patients.AddAsync(patient, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return patient;
        }

        var patientSarah = await EnsurePatientAsync(
            pat1User,
            new DateOnly(1995, 5, 15),
            Gender.Female,
            "+201011112222",
            "A+",
            "Penicillin, Peanuts (Moderate)",
            "Mild Bronchial Asthma");

        var patientOmar = await EnsurePatientAsync(
            pat2User,
            new DateOnly(1988, 11, 20),
            Gender.Male,
            "+201022223333",
            "O+",
            "None Known (NKDA)",
            "Primary Essential Hypertension (Stage 1)");

        var patientLayla = await EnsurePatientAsync(
            pat3User,
            new DateOnly(2001, 3, 8),
            Gender.Female,
            "+201033334444",
            "B+",
            "Sulfa Drugs, Aspirin",
            "None");

        var patientYoussef = await EnsurePatientAsync(
            pat4User,
            new DateOnly(2018, 9, 12),
            Gender.Male,
            "+201044445555",
            "AB+",
            "Seasonal Pollen, Dust Mites",
            "Allergic Rhinitis");

        // =========================================================================
        // 7. APPOINTMENTS (Varied Statuses & Today's Active Queue)
        // =========================================================================
        var existingAppointmentsCount = await _dbContext.Appointments
            .IgnoreQueryFilters()
            .CountAsync(cancellationToken);

        Appointment apptOmar;
        Appointment apptLayla;
        Appointment apptSarahPast;

        if (existingAppointmentsCount < 3)
        {
            var now = DateTime.UtcNow;
            var today = DateTime.UtcNow.Date;

            // 1. Omar: In Consultation with Dr. Ahmad today (Queue #1)
            var startOmar = today.AddHours(9).AddMinutes(30);
            apptOmar = Appointment.Create(
                defaultTenant.Id,
                drAhmad.Id,
                patientOmar.Id,
                centralClinic.Id,
                DateTimeRange.Create(startOmar, startOmar.AddMinutes(30)),
                now.AddDays(-1));
            apptOmar.Confirm();
            apptOmar.CheckIn(1);
            apptOmar.StartConsultation();
            await _dbContext.Appointments.AddAsync(apptOmar, cancellationToken);

            // 2. Layla: Checked-in and Waiting in reception queue (Queue #2)
            var startLayla = today.AddHours(10).AddMinutes(15);
            apptLayla = Appointment.Create(
                defaultTenant.Id,
                drAhmad.Id,
                patientLayla.Id,
                centralClinic.Id,
                DateTimeRange.Create(startLayla, startLayla.AddMinutes(30)),
                now.AddDays(-2));
            apptLayla.Confirm();
            apptLayla.CheckIn(2);
            await _dbContext.Appointments.AddAsync(apptLayla, cancellationToken);

            // 3. Sarah: Completed consultation with Dr. Ahmad 2 days ago
            var startSarah = now.AddDays(-2).Date.AddHours(11);
            apptSarahPast = Appointment.Create(
                defaultTenant.Id,
                drAhmad.Id,
                patientSarah.Id,
                centralClinic.Id,
                DateTimeRange.Create(startSarah, startSarah.AddMinutes(30)),
                now.AddDays(-5));
            apptSarahPast.Confirm();
            apptSarahPast.CheckIn(1);
            apptSarahPast.StartConsultation();
            apptSarahPast.Complete();
            await _dbContext.Appointments.AddAsync(apptSarahPast, cancellationToken);

            // 4. Youssef: Scheduled with Dr. Mona tomorrow
            var startYoussef = now.AddDays(1).Date.AddHours(12);
            var apptYoussef = Appointment.Create(
                defaultTenant.Id,
                drMona.Id,
                patientYoussef.Id,
                centralClinic.Id,
                DateTimeRange.Create(startYoussef, startYoussef.AddMinutes(30)),
                now.AddDays(-1));
            apptYoussef.Confirm();
            await _dbContext.Appointments.AddAsync(apptYoussef, cancellationToken);

            // 5. Sarah: Cancelled appointment with Dr. Tarek
            var startCancelled = now.AddDays(-4).Date.AddHours(14);
            var apptCancelled = Appointment.Create(
                defaultTenant.Id,
                drTarek.Id,
                patientSarah.Id,
                nileClinic.Id,
                DateTimeRange.Create(startCancelled, startCancelled.AddMinutes(30)),
                now.AddDays(-7));
            apptCancelled.CancelByPatient("Work schedule conflict", 24, now.AddDays(-6));
            await _dbContext.Appointments.AddAsync(apptCancelled, cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        else
        {
            apptOmar = await _dbContext.Appointments.IgnoreQueryFilters().FirstAsync(cancellationToken);
            apptLayla = apptOmar;
            apptSarahPast = apptOmar;
        }

        // =========================================================================
        // 8. CONSULTATIONS, CLINICAL NOTES & VITALS
        // =========================================================================
        if (!await _dbContext.ConsultationNotes.IgnoreQueryFilters().AnyAsync(cancellationToken))
        {
            var noteOmar = new ConsultationNote(
                Guid.NewGuid(),
                defaultTenant.Id,
                apptOmar.Id,
                patientOmar.Id,
                drAhmad.Id,
                centralClinic.Id,
                chiefComplaint: "Exertional chest heaviness, mild shortness of breath, and morning headache.",
                examinationNotes: "Subjective: 38yo male presenting with 3-week history of retrosternal discomfort during brisk walking.\nObjective: S1 and S2 audible, no audible heart murmurs or friction rubs. Chest bilaterally clear to auscultation without wheezes.\nAssessment: Primary Essential Hypertension Stage 1 with exercise intolerance. R/O ischemic coronary etiology.\nPlan: Prescribed beta-blocker and statin therapy. Advised DASH low-sodium diet and daily brisk walking.",
                diagnosis: "Essential (primary) hypertension - ICD-10 I10",
                treatmentPlan: "Start Concor 5mg QD and Crestor 10mg QHS. Order lipid panel and 12-lead ECG. Return visit in 14 days.",
                consultationDateUtc: DateTime.UtcNow,
                followUpDateUtc: DateTime.UtcNow.AddDays(14),
                labRequests: "12-Lead Resting ECG, Fasting Lipid Panel (Total Cholesterol, HDL, LDL, Triglycerides), Serum Creatinine, Electrolytes");

            await _dbContext.ConsultationNotes.AddAsync(noteOmar, cancellationToken);

            // Vitals for Omar
            var vitalsOmar = new PatientVitalSigns(
                Guid.NewGuid(),
                defaultTenant.Id,
                patientOmar.Id,
                noteOmar.Id,
                systolicBp: 138,
                diastolicBp: 88,
                heartRate: 82,
                temperatureCelsius: 36.8m,
                weightKg: 84.5m,
                heightCm: 176m,
                bloodGlucoseMgDl: 108m,
                recordedAtUtc: DateTime.UtcNow);

            await _dbContext.PatientVitalSigns.AddAsync(vitalsOmar, cancellationToken);

            // Vitals for Sarah
            var vitalsSarah = new PatientVitalSigns(
                Guid.NewGuid(),
                defaultTenant.Id,
                patientSarah.Id,
                null,
                systolicBp: 118,
                diastolicBp: 76,
                heartRate: 72,
                temperatureCelsius: 36.6m,
                weightKg: 62.0m,
                heightCm: 165m,
                bloodGlucoseMgDl: 92m,
                recordedAtUtc: DateTime.UtcNow.AddDays(-2));

            await _dbContext.PatientVitalSigns.AddAsync(vitalsSarah, cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        // =========================================================================
        // 9. PRESCRIPTIONS & MEDICATIONS
        // =========================================================================
        if (!await _dbContext.Prescriptions.IgnoreQueryFilters().AnyAsync(cancellationToken))
        {
            var rxOmar = Prescription.Create(
                defaultTenant.Id,
                null,
                patientOmar.Id,
                drAhmad.Id,
                centralClinic.Id,
                diagnosis: "Essential Hypertension Stage 1 (ICD-10 I10)",
                generalInstructions: "Take medications at the same time each morning with plenty of water. Monitor blood pressure daily.");

            rxOmar.AddItem("Concor (Bisoprolol Fumarate)", "5 mg", "Once Daily (Morning)", "30 Days", "Take 1 tablet before breakfast");
            rxOmar.AddItem("Crestor (Rosuvastatin Calcium)", "10 mg", "Once Daily (Bedtime)", "30 Days", "Take 1 tablet after dinner");
            rxOmar.AddItem("Aspirin Protect (Acetylsalicylic Acid)", "100 mg", "Once Daily (After Lunch)", "30 Days", "Take with a full meal");

            await _dbContext.Prescriptions.AddAsync(rxOmar, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        // =========================================================================
        // 10. BILLING, INVOICES & PAYMENTS (Including Partial Payments)
        // =========================================================================
        if (!await _dbContext.Invoices.IgnoreQueryFilters().AnyAsync(cancellationToken))
        {
            // Invoice 1: Fully Paid (Sarah)
            var inv1 = Invoice.Create(
                defaultTenant.Id,
                apptSarahPast.Id,
                patientSarah.Id,
                centralClinic.Id,
                discountAmount: 0,
                taxAmount: 0,
                paymentMethod: PaymentMethod.Cash);

            inv1.AddItem("Specialist Clinical Consultation", 1, 400m);
            inv1.AddItem("12-Lead Diagnostic Electrocardiogram (ECG)", 1, 200m);
            inv1.RecordPayment(600m, PaymentMethod.Cash, DateTime.UtcNow.AddDays(-2));
            await _dbContext.Invoices.AddAsync(inv1, cancellationToken);

            // Invoice 2: Partially Paid with Outstanding Balance (Omar)
            var inv2 = Invoice.Create(
                defaultTenant.Id,
                apptOmar.Id,
                patientOmar.Id,
                centralClinic.Id,
                discountAmount: 50m,
                taxAmount: 0,
                paymentMethod: PaymentMethod.CreditCard);

            inv2.AddItem("Comprehensive Cardiology Consultation", 1, 500m);
            inv2.AddItem("Complete Lipid Profile & Metabolic Lab Panel", 1, 450m);
            inv2.RecordPayment(500m, PaymentMethod.CreditCard, DateTime.UtcNow); // Remaining: 400 EGP
            await _dbContext.Invoices.AddAsync(inv2, cancellationToken);

            // Invoice 3: Issued / Pending Payment (Layla)
            var inv3 = Invoice.Create(
                defaultTenant.Id,
                apptLayla.Id,
                patientLayla.Id,
                centralClinic.Id,
                discountAmount: 0,
                taxAmount: 0,
                paymentMethod: PaymentMethod.Cash);

            inv3.AddItem("Routine Outpatient Consultation", 1, 350m);
            await _dbContext.Invoices.AddAsync(inv3, cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        // =========================================================================
        // 11. MEDICAL ATTACHMENTS
        // =========================================================================
        if (!await _dbContext.MedicalAttachments.IgnoreQueryFilters().AnyAsync(cancellationToken))
        {
            var att1 = MedicalAttachment.Create(
                patientSarah.Id,
                "Complete Blood Count (CBC) Panel",
                "LabResult",
                "cbc_sarah_ali.pdf",
                "application/pdf",
                245760,
                "data:application/pdf;base64,JVBERi0xLjQKJ...",
                "Normal hemoglobin 13.5 g/dL, normal WBC count 6.8 x10^3/uL.",
                defaultTenant.Id);

            var att2 = MedicalAttachment.Create(
                patientOmar.Id,
                "Chest X-Ray PA View",
                "XRay",
                "chest_xray_omar.png",
                "image/png",
                524288,
                "data:image/png;base64,iVBORw0KGgo...",
                "Cardiothoracic ratio within normal limits, clear costophrenic angles.",
                defaultTenant.Id);

            await _dbContext.MedicalAttachments.AddRangeAsync(new[] { att1, att2 }, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        // =========================================================================
        // 12. INSURANCE PROVIDERS
        // =========================================================================
        if (!await _dbContext.InsuranceProviders.IgnoreQueryFilters().AnyAsync(cancellationToken))
        {
            var providers = new List<InsuranceProvider>
            {
                InsuranceProvider.Create("Bupa Global Healthcare", "بوبا للتأمين الصحي العالمي", "BUPA-EGY", 80m, "+20224001000", defaultTenant.Id),
                InsuranceProvider.Create("AXA Insurance Egypt", "أكسا للتأمين مصر", "AXA-EGY", 85m, "+20224002000", defaultTenant.Id),
                InsuranceProvider.Create("MedNet Healthcare Solutions", "ميدنت لإدارة النفقات الطبية", "MEDNET", 75m, "+20224003000", defaultTenant.Id),
                InsuranceProvider.Create("MetLife Health Care", "متلايف للرعاية الصحية", "METLIFE", 80m, "+20224004000", defaultTenant.Id)
            };
            await _dbContext.InsuranceProviders.AddRangeAsync(providers, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        // =========================================================================
        // 13. MEDICAL INVENTORY & CONSUMABLES
        // =========================================================================
        if (!await _dbContext.MedicalInventoryItems.IgnoreQueryFilters().AnyAsync(cancellationToken))
        {
            var inventoryItems = new List<MedicalInventoryItem>
            {
                MedicalInventoryItem.Create(centralClinic.Id, "Sterile Disposable Syringes 5ml", "سرنجات معقمة 5 مل استخدام واحد", "Consumables", 150, 30, "Box", 45m, "BATCH-2026-A", DateTime.UtcNow.AddMonths(18), defaultTenant.Id),
                MedicalInventoryItem.Create(centralClinic.Id, "Sterile Gauze Swabs 10x10cm", "شاش طبي معقم 10*10 سم", "Consumables", 90, 25, "Pack", 25m, "BATCH-2026-B", DateTime.UtcNow.AddMonths(12), defaultTenant.Id),
                MedicalInventoryItem.Create(centralClinic.Id, "Surgical Disposable Face Masks (50pcs)", "كمامات طبية جراحية 3 طبقات", "Consumables", 12, 20, "Box", 35m, "BATCH-2026-C", DateTime.UtcNow.AddMonths(24), defaultTenant.Id), // Low stock
                MedicalInventoryItem.Create(centralClinic.Id, "Examination Nitrile Gloves Powder-Free (M)", "قفازات نيتريل فحص طبي مقاس وسط", "Consumables", 60, 15, "Box", 95m, "BATCH-2026-D", DateTime.UtcNow.AddMonths(20), defaultTenant.Id),
                MedicalInventoryItem.Create(centralClinic.Id, "Normal Saline IV Infusion 0.9% 500ml", "محلول ملحي وريدي 0.9% 500 مل", "Pharmaceuticals", 70, 20, "Bottle", 30m, "BATCH-2026-E", DateTime.UtcNow.AddMonths(15), defaultTenant.Id),
                MedicalInventoryItem.Create(centralClinic.Id, "Medical Antiseptic Ethyl Alcohol 70% 1L", "كحول طبي مطهر 70% 1 لتر", "Supplies", 6, 10, "Bottle", 55m, "BATCH-2026-F", DateTime.UtcNow.AddMonths(14), defaultTenant.Id) // Low stock
            };
            await _dbContext.MedicalInventoryItems.AddRangeAsync(inventoryItems, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        // =========================================================================
        // 14. NOTIFICATION REMINDER LOGS (WhatsApp, SMS, Email)
        // =========================================================================
        if (!await _dbContext.NotificationLogs.IgnoreQueryFilters().AnyAsync(cancellationToken))
        {
            var notif1 = new NotificationLog(
                Guid.NewGuid(),
                defaultTenant.Id,
                apptOmar.Id,
                "Omar Khaled",
                "+201022223333",
                "patient.omar@example.com",
                NotificationChannel.WhatsApp,
                NotificationType.AppointmentReminder,
                NotificationStatus.Delivered,
                "مرحباً Omar Khaled 🌸 نذكرك بموعدك لدى Apex Central Medical Center مع د. Ahmad Hassan اليوم الساعة 09:30 AM.",
                DateTime.UtcNow.AddHours(-2),
                "WhatsApp Appointment Reminder Link",
                null);

            var notif2 = new NotificationLog(
                Guid.NewGuid(),
                defaultTenant.Id,
                apptSarahPast.Id,
                "Sarah Ali",
                "+201011112222",
                "patient.sarah@example.com",
                NotificationChannel.Sms,
                NotificationType.BookingConfirmation,
                NotificationStatus.Sent,
                "تم تأكيد حجز موعدك بنجاح مع د. Ahmad Hassan في عيادة أبيكس المركزية.",
                DateTime.UtcNow.AddDays(-2),
                "SMS Booking Confirmation",
                null);

            var notif3 = new NotificationLog(
                Guid.NewGuid(),
                defaultTenant.Id,
                apptLayla.Id,
                "Layla Mahmoud",
                "+201033334444",
                "patient.layla@example.com",
                NotificationChannel.Email,
                NotificationType.AppointmentReminder,
                NotificationStatus.Sent,
                "Dear Layla Mahmoud, friendly reminder for your consultation token #02 at Apex Central Clinic.",
                DateTime.UtcNow.AddMinutes(-45),
                "Appointment Reminder - Apex Central",
                null);

            await _dbContext.NotificationLogs.AddRangeAsync(new[] { notif1, notif2, notif3 }, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        // =========================================================================
        // 15. AUDIT LOGS (Compliance Trail)
        // =========================================================================
        if (!await _dbContext.AuditLogs.IgnoreQueryFilters().AnyAsync(cancellationToken))
        {
            var audit1 = new AuditLog(
                Guid.NewGuid(),
                defaultTenant.Id,
                "Patient",
                patientOmar.Id.ToString(),
                "Created",
                adminUser.Email.Value,
                DateTime.UtcNow.AddDays(-1),
                "{\"BloodGroup\":\"O+\",\"Allergies\":\"None Known\",\"ChronicConditions\":\"Hypertension\"}");

            var audit2 = new AuditLog(
                Guid.NewGuid(),
                defaultTenant.Id,
                "ConsultationNote",
                apptOmar.Id.ToString(),
                "Created",
                doc1User.Email.Value,
                DateTime.UtcNow,
                "{\"Diagnosis\":\"Essential (primary) hypertension\",\"FollowUpDays\":14}");

            var audit3 = new AuditLog(
                Guid.NewGuid(),
                defaultTenant.Id,
                "Invoice",
                patientOmar.Id.ToString(),
                "PartialPayment",
                receptionistUser.Email.Value,
                DateTime.UtcNow,
                "{\"PaidAmount\":500.00,\"Remaining\":400.00,\"PaymentMethod\":\"CreditCard\"}");

            await _dbContext.AuditLogs.AddRangeAsync(new[] { audit1, audit2, audit3 }, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Comprehensive clinic ecosystem seeding completed successfully!");
    }
}
