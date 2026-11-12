using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ClinicBooking.API.Controllers;
using ClinicBooking.Application.Contracts.Services;
using ClinicBooking.Application.Features.Appointments.Commands.BookAppointment;
using ClinicBooking.Application.Features.Appointments.Commands.CancelAppointment;
using ClinicBooking.Application.Features.Appointments.DTOs;
using ClinicBooking.Application.Features.Authentication.Commands.Login;
using ClinicBooking.Application.Features.Authentication.DTOs;
using ClinicBooking.Domain.Entities;
using ClinicBooking.Domain.Enums;
using ClinicBooking.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicBooking.API.Tests.Controllers;

public class AppointmentsConcurrencyTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AppointmentsConcurrencyTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task BookAppointment_ConcurrentRequestsForSameDoctorSlot_ExactlyOneSucceedsAndOneConflicts()
    {
        // 0. Ensure WebApplicationFactory server is started
        _ = _factory.CreateClient();

        // 1. Arrange Data via Scoped DbContext
        Guid tenantId;
        Guid doctorId;
        Guid clinicId;
        Guid patient1Id;
        Guid patient2Id;
        string patient1Email = $"patient_race_1_{Guid.NewGuid():N}@clinic.com";
        string patient2Email = $"patient_race_2_{Guid.NewGuid():N}@clinic.com";
        string password = "RacePassword123!";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClinicBookingDbContext>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            var tenant = await db.Tenants.IgnoreQueryFilters().FirstAsync(t => t.Identifier == _factory.DefaultTenantSlug);
            tenantId = tenant.Id;

            var clinic = await db.Clinics.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.TenantId == tenantId);
            if (clinic is null)
            {
                clinic = Clinic.Create(tenantId, "Test Clinic", "123 Main St", "Cairo", "+201000000000");
                await db.Clinics.AddAsync(clinic);
                await db.SaveChangesAsync();
            }
            clinicId = clinic.Id;

            var specialty = await db.Specialties.IgnoreQueryFilters().FirstAsync();

            // Create Doctor User & Entity
            var doctorUser = User.Create($"doctor_race_{Guid.NewGuid():N}@clinic.com", "Doctor", "Who", UserRole.Doctor, tenantId);
            doctorUser.SetPasswordHash(passwordHasher.HashPassword(password));
            await db.Users.AddAsync(doctorUser);

            var doctor = new Doctor(
                Guid.NewGuid(),
                tenantId,
                doctorUser.Id,
                specialty.Id,
                $"LIC-{Guid.NewGuid():N}"[..12],
                "Expert in cardiology",
                defaultSlotDurationMinutes: 30);

            // Give Doctor full availability on Mondays to Fridays 08:00 - 18:00
            for (var day = DayOfWeek.Monday; day <= DayOfWeek.Friday; day++)
            {
                doctor.AddAvailability(clinicId, day, new TimeOnly(8, 0), new TimeOnly(18, 0));
            }

            await db.Doctors.AddAsync(doctor);
            doctorId = doctor.Id;

            // Create Patient 1
            var user1 = User.Create(patient1Email, "PatientOne", "Race", UserRole.Patient, tenantId);
            user1.SetPasswordHash(passwordHasher.HashPassword(password));
            await db.Users.AddAsync(user1);

            var p1 = Patient.Create(tenantId, user1.Id, new DateOnly(1990, 1, 1), Gender.Male);
            await db.Patients.AddAsync(p1);
            patient1Id = p1.Id;

            // Create Patient 2
            var user2 = User.Create(patient2Email, "PatientTwo", "Race", UserRole.Patient, tenantId);
            user2.SetPasswordHash(passwordHasher.HashPassword(password));
            await db.Users.AddAsync(user2);

            var p2 = Patient.Create(tenantId, user2.Id, new DateOnly(1992, 2, 2), Gender.Female);
            await db.Patients.AddAsync(p2);
            patient2Id = p2.Id;

            await db.SaveChangesAsync();
        }

        // 2. Authenticate both patients and obtain JWT tokens
        var client1 = _factory.CreateClient();
        client1.DefaultRequestHeaders.Add("X-Tenant-Slug", _factory.DefaultTenantSlug);

        var login1Response = await client1.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(patient1Email, password));
        Assert.Equal(HttpStatusCode.OK, login1Response.StatusCode);
        var auth1 = await login1Response.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(auth1);
        client1.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth1.AccessToken);

        var client2 = _factory.CreateClient();
        client2.DefaultRequestHeaders.Add("X-Tenant-Slug", _factory.DefaultTenantSlug);

        var login2Response = await client2.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(patient2Email, password));
        Assert.Equal(HttpStatusCode.OK, login2Response.StatusCode);
        var auth2 = await login2Response.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(auth2);
        client2.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth2.AccessToken);

        // 3. Define target appointment slot (next Monday at 10:00 UTC)
        var today = DateTime.UtcNow.Date;
        var daysUntilNextMonday = ((int)DayOfWeek.Monday - (int)today.DayOfWeek + 7) % 7;
        if (daysUntilNextMonday == 0) daysUntilNextMonday = 7;
        var nextMonday = today.AddDays(daysUntilNextMonday);
        var slotStart = DateTime.SpecifyKind(nextMonday.AddHours(10), DateTimeKind.Utc);
        var slotEnd = DateTime.SpecifyKind(nextMonday.AddHours(10).AddMinutes(30), DateTimeKind.Utc);

        var command1 = new BookAppointmentCommand(
            tenantId,
            doctorId,
            patient1Id,
            clinicId,
            slotStart,
            slotEnd,
            Notes: "Patient 1 racing request");

        var command2 = new BookAppointmentCommand(
            tenantId,
            doctorId,
            patient2Id,
            clinicId,
            slotStart,
            slotEnd,
            Notes: "Patient 2 racing request");

        // 4. Act: Dispatch concurrent HTTP requests racing for the same slot
        var task1 = client1.PostAsJsonAsync("/api/v1/appointments", command1);
        var task2 = client2.PostAsJsonAsync("/api/v1/appointments", command2);

        var responses = await Task.WhenAll(task1, task2);

        // 5. Assert: One succeeded, one was rejected with 409 Conflict
        var successResponses = responses.Where(r => r.StatusCode == HttpStatusCode.OK).ToList();
        var conflictResponses = responses.Where(r => r.StatusCode == HttpStatusCode.Conflict).ToList();

        Assert.Single(successResponses);
        Assert.Single(conflictResponses);

        var successAppointment = await successResponses[0].Content.ReadFromJsonAsync<AppointmentDto>();
        Assert.NotNull(successAppointment);
        Assert.Equal(doctorId, successAppointment.DoctorId);
        Assert.Equal(slotStart, successAppointment.StartTimeUtc);

        var conflictProblem = await conflictResponses[0].Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(conflictProblem);
        Assert.Equal(StatusCodes.Status409Conflict, conflictProblem.Status);

        // Verify the database contains strictly 1 appointment for that doctor at that slot
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClinicBookingDbContext>();
            var totalBooked = await db.Appointments
                .IgnoreQueryFilters()
                .CountAsync(a => a.DoctorId == doctorId && a.StartTimeUtc == slotStart);

            Assert.Equal(1, totalBooked);
        }
    }

    [Fact]
    public async Task BookAppointment_PatientAttemptsDoubleBooking_ReturnsConflict()
    {
        // 0. Ensure server is started
        _ = _factory.CreateClient();

        // 1. Arrange Data
        Guid tenantId;
        Guid doctor1Id;
        Guid doctor2Id;
        Guid clinicId;
        Guid patientId;
        string patientEmail = $"patient_double_{Guid.NewGuid():N}@clinic.com";
        string password = "DoublePassword123!";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClinicBookingDbContext>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            var tenant = await db.Tenants.IgnoreQueryFilters().FirstAsync(t => t.Identifier == _factory.DefaultTenantSlug);
            tenantId = tenant.Id;

            var clinic = await db.Clinics.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.TenantId == tenantId);
            if (clinic is null)
            {
                clinic = Clinic.Create(tenantId, "Test Clinic", "123 Main St", "Cairo", "+201000000000");
                await db.Clinics.AddAsync(clinic);
                await db.SaveChangesAsync();
            }
            clinicId = clinic.Id;

            var specialty = await db.Specialties.IgnoreQueryFilters().FirstAsync();

            // Doctor 1
            var d1User = User.Create($"d1_{Guid.NewGuid():N}@clinic.com", "Doctor1", "Smith", UserRole.Doctor, tenantId);
            d1User.SetPasswordHash(passwordHasher.HashPassword(password));
            await db.Users.AddAsync(d1User);

            var doc1 = new Doctor(Guid.NewGuid(), tenantId, d1User.Id, specialty.Id, $"LIC1-{Guid.NewGuid():N}"[..12], "Cardiologist");
            doc1.AddAvailability(clinicId, DayOfWeek.Wednesday, new TimeOnly(8, 0), new TimeOnly(18, 0));
            await db.Doctors.AddAsync(doc1);
            doctor1Id = doc1.Id;

            // Doctor 2
            var d2User = User.Create($"d2_{Guid.NewGuid():N}@clinic.com", "Doctor2", "Jones", UserRole.Doctor, tenantId);
            d2User.SetPasswordHash(passwordHasher.HashPassword(password));
            await db.Users.AddAsync(d2User);

            var doc2 = new Doctor(Guid.NewGuid(), tenantId, d2User.Id, specialty.Id, $"LIC2-{Guid.NewGuid():N}"[..12], "Neurologist");
            doc2.AddAvailability(clinicId, DayOfWeek.Wednesday, new TimeOnly(8, 0), new TimeOnly(18, 0));
            await db.Doctors.AddAsync(doc2);
            doctor2Id = doc2.Id;

            // Patient
            var pUser = User.Create(patientEmail, "Patient", "Double", UserRole.Patient, tenantId);
            pUser.SetPasswordHash(passwordHasher.HashPassword(password));
            await db.Users.AddAsync(pUser);

            var patient = Patient.Create(tenantId, pUser.Id, new DateOnly(1988, 8, 8), Gender.Male);
            await db.Patients.AddAsync(patient);
            patientId = patient.Id;

            await db.SaveChangesAsync();
        }

        // 2. Authenticate Patient
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Slug", _factory.DefaultTenantSlug);

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(patientEmail, password));
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(auth);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        // Next Wednesday 14:00 - 14:30
        var today = DateTime.UtcNow.Date;
        var daysUntilNextWed = ((int)DayOfWeek.Wednesday - (int)today.DayOfWeek + 7) % 7;
        if (daysUntilNextWed == 0) daysUntilNextWed = 7;
        var nextWed = today.AddDays(daysUntilNextWed);
        var slotStart = DateTime.SpecifyKind(nextWed.AddHours(14), DateTimeKind.Utc);
        var slotEnd = DateTime.SpecifyKind(nextWed.AddHours(14).AddMinutes(30), DateTimeKind.Utc);

        var cmd1 = new BookAppointmentCommand(tenantId, doctor1Id, patientId, clinicId, slotStart, slotEnd, "First appointment");
        var cmd2 = new BookAppointmentCommand(tenantId, doctor2Id, patientId, clinicId, slotStart, slotEnd, "Second appointment at same time");

        // 3. Act: Book Doctor 1 first
        var response1 = await client.PostAsJsonAsync("/api/v1/appointments", cmd1);
        var r1Body = await response1.Content.ReadAsStringAsync();
        Assert.True(response1.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response1.StatusCode}. Body: {r1Body}");

        // 4. Act: Attempt to book Doctor 2 at the exact same overlapping time
        var response2 = await client.PostAsJsonAsync("/api/v1/appointments", cmd2);

        // 5. Assert: Patient double-booking is rejected with 409 Conflict
        Assert.Equal(HttpStatusCode.Conflict, response2.StatusCode);
        var problem = await response2.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status409Conflict, problem.Status);
    }

    [Fact]
    public async Task BookAppointment_OverlappingIntervalsDifferentStartTimes_ForSameDoctor_ReturnsConflict()
    {
        _ = _factory.CreateClient();

        Guid tenantId;
        Guid doctorId;
        Guid clinicId;
        Guid patient1Id;
        Guid patient2Id;
        string p1Email = $"overlap_p1_{Guid.NewGuid():N}@clinic.com";
        string p2Email = $"overlap_p2_{Guid.NewGuid():N}@clinic.com";
        string password = "OverlapPassword123!";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClinicBookingDbContext>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            var tenant = await db.Tenants.IgnoreQueryFilters().FirstAsync(t => t.Identifier == _factory.DefaultTenantSlug);
            tenantId = tenant.Id;

            var clinic = await db.Clinics.IgnoreQueryFilters().FirstAsync(c => c.TenantId == tenantId);
            clinicId = clinic.Id;

            var specialty = await db.Specialties.IgnoreQueryFilters().FirstAsync();

            var docUser = User.Create($"doc_overlap_{Guid.NewGuid():N}@clinic.com", "Doctor", "Overlap", UserRole.Doctor, tenantId);
            docUser.SetPasswordHash(passwordHasher.HashPassword(password));
            await db.Users.AddAsync(docUser);

            var doctor = new Doctor(Guid.NewGuid(), tenantId, docUser.Id, specialty.Id, $"OVL-{Guid.NewGuid():N}"[..12], "Specialist");
            doctor.AddAvailability(clinicId, DayOfWeek.Thursday, new TimeOnly(8, 0), new TimeOnly(18, 0));
            await db.Doctors.AddAsync(doctor);
            doctorId = doctor.Id;

            var u1 = User.Create(p1Email, "P1", "Overlap", UserRole.Patient, tenantId);
            u1.SetPasswordHash(passwordHasher.HashPassword(password));
            await db.Users.AddAsync(u1);
            var p1 = Patient.Create(tenantId, u1.Id, new DateOnly(1990, 5, 5), Gender.Male);
            await db.Patients.AddAsync(p1);
            patient1Id = p1.Id;

            var u2 = User.Create(p2Email, "P2", "Overlap", UserRole.Patient, tenantId);
            u2.SetPasswordHash(passwordHasher.HashPassword(password));
            await db.Users.AddAsync(u2);
            var p2 = Patient.Create(tenantId, u2.Id, new DateOnly(1991, 6, 6), Gender.Female);
            await db.Patients.AddAsync(p2);
            patient2Id = p2.Id;

            await db.SaveChangesAsync();
        }

        var client1 = _factory.CreateClient();
        client1.DefaultRequestHeaders.Add("X-Tenant-Slug", _factory.DefaultTenantSlug);
        var auth1 = await (await client1.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(p1Email, password))).Content.ReadFromJsonAsync<AuthResponseDto>();
        client1.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth1!.AccessToken);

        var client2 = _factory.CreateClient();
        client2.DefaultRequestHeaders.Add("X-Tenant-Slug", _factory.DefaultTenantSlug);
        var auth2 = await (await client2.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(p2Email, password))).Content.ReadFromJsonAsync<AuthResponseDto>();
        client2.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth2!.AccessToken);

        var today = DateTime.UtcNow.Date;
        var daysUntilNextThu = ((int)DayOfWeek.Thursday - (int)today.DayOfWeek + 7) % 7;
        if (daysUntilNextThu == 0) daysUntilNextThu = 7;
        var nextThu = today.AddDays(daysUntilNextThu);

        // Appointment A: 10:00 - 10:30
        var startA = DateTime.SpecifyKind(nextThu.AddHours(10), DateTimeKind.Utc);
        var endA = DateTime.SpecifyKind(nextThu.AddHours(10).AddMinutes(30), DateTimeKind.Utc);

        // Appointment B: 10:15 - 10:45 (Different StartTimeUtc, overlapping interval!)
        var startB = DateTime.SpecifyKind(nextThu.AddHours(10).AddMinutes(15), DateTimeKind.Utc);
        var endB = DateTime.SpecifyKind(nextThu.AddHours(10).AddMinutes(45), DateTimeKind.Utc);

        var resA = await client1.PostAsJsonAsync("/api/v1/appointments",
            new BookAppointmentCommand(tenantId, doctorId, patient1Id, clinicId, startA, endA, "10:00 to 10:30"));
        Assert.Equal(HttpStatusCode.OK, resA.StatusCode);

        var resB = await client2.PostAsJsonAsync("/api/v1/appointments",
            new BookAppointmentCommand(tenantId, doctorId, patient2Id, clinicId, startB, endB, "10:15 to 10:45 overlapping"));
        Assert.Equal(HttpStatusCode.Conflict, resB.StatusCode);
    }

    [Fact]
    public async Task BookAppointment_CancelFollowedByRebookingSameSlot_Succeeds()
    {
        _ = _factory.CreateClient();

        Guid tenantId;
        Guid doctorId;
        Guid clinicId;
        Guid patientId;
        string pEmail = $"cancel_rebook_{Guid.NewGuid():N}@clinic.com";
        string password = "RebookPassword123!";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClinicBookingDbContext>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            var tenant = await db.Tenants.IgnoreQueryFilters().FirstAsync(t => t.Identifier == _factory.DefaultTenantSlug);
            tenantId = tenant.Id;
            var clinic = await db.Clinics.IgnoreQueryFilters().FirstAsync(c => c.TenantId == tenantId);
            clinicId = clinic.Id;
            var specialty = await db.Specialties.IgnoreQueryFilters().FirstAsync();

            var docUser = User.Create($"doc_rebook_{Guid.NewGuid():N}@clinic.com", "Doctor", "Rebook", UserRole.Doctor, tenantId);
            docUser.SetPasswordHash(passwordHasher.HashPassword(password));
            await db.Users.AddAsync(docUser);

            var doctor = new Doctor(Guid.NewGuid(), tenantId, docUser.Id, specialty.Id, $"RBK-{Guid.NewGuid():N}"[..12], "Specialist");
            doctor.AddAvailability(clinicId, DayOfWeek.Friday, new TimeOnly(8, 0), new TimeOnly(18, 0));
            await db.Doctors.AddAsync(doctor);
            doctorId = doctor.Id;

            var u1 = User.Create(pEmail, "P1", "Rebook", UserRole.Patient, tenantId);
            u1.SetPasswordHash(passwordHasher.HashPassword(password));
            await db.Users.AddAsync(u1);
            var p1 = Patient.Create(tenantId, u1.Id, new DateOnly(1993, 3, 3), Gender.Male);
            await db.Patients.AddAsync(p1);
            patientId = p1.Id;

            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Slug", _factory.DefaultTenantSlug);
        var auth = await (await client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(pEmail, password))).Content.ReadFromJsonAsync<AuthResponseDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        var today = DateTime.UtcNow.Date;
        var daysUntilNextFri = ((int)DayOfWeek.Friday - (int)today.DayOfWeek + 7) % 7;
        if (daysUntilNextFri <= 2) daysUntilNextFri += 7;
        var nextFri = today.AddDays(daysUntilNextFri);

        var start = DateTime.SpecifyKind(nextFri.AddHours(11), DateTimeKind.Utc);
        var end = DateTime.SpecifyKind(nextFri.AddHours(11).AddMinutes(30), DateTimeKind.Utc);

        // 1. Book initial appointment
        var bookRes1 = await client.PostAsJsonAsync("/api/v1/appointments",
            new BookAppointmentCommand(tenantId, doctorId, patientId, clinicId, start, end, "Initial booking"));
        Assert.Equal(HttpStatusCode.OK, bookRes1.StatusCode);
        var booked = await bookRes1.Content.ReadFromJsonAsync<AppointmentDto>();
        Assert.NotNull(booked);

        // 2. Cancel initial appointment
        var cancelRes = await client.PostAsJsonAsync($"/api/v1/appointments/{booked.Id}/cancel",
            new CancelAppointmentRequest("Patient requested cancellation"));
        Assert.Equal(HttpStatusCode.OK, cancelRes.StatusCode);

        // 3. Rebook the exact same slot -> must succeed while preserving cancelled history
        var bookRes2 = await client.PostAsJsonAsync("/api/v1/appointments",
            new BookAppointmentCommand(tenantId, doctorId, patientId, clinicId, start, end, "Rebooked after cancellation"));
        Assert.Equal(HttpStatusCode.OK, bookRes2.StatusCode);
    }

    [Fact]
    public async Task ReceptionAndPublicBooking_WalkInAndSocialAttribution_SucceedsWithFullMetadata()
    {
        _ = _factory.CreateClient();

        Guid tenantId;
        Guid doctorId;
        Guid clinicId;
        Guid patientId;
        string receptionistEmail = $"reception_{Guid.NewGuid():N}@clinic.com";
        string password = "ReceptionPassword123!";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClinicBookingDbContext>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            var tenant = await db.Tenants.IgnoreQueryFilters().FirstAsync(t => t.Identifier == _factory.DefaultTenantSlug);
            tenantId = tenant.Id;
            var clinic = await db.Clinics.IgnoreQueryFilters().FirstAsync(c => c.TenantId == tenantId);
            clinicId = clinic.Id;
            var specialty = await db.Specialties.IgnoreQueryFilters().FirstAsync();

            var docUser = User.Create($"doc_rec_{Guid.NewGuid():N}@clinic.com", "Doctor", "Rec", UserRole.Doctor, tenantId);
            docUser.SetPasswordHash(passwordHasher.HashPassword(password));
            await db.Users.AddAsync(docUser);

            var doctor = new Doctor(Guid.NewGuid(), tenantId, docUser.Id, specialty.Id, $"REC-{Guid.NewGuid():N}"[..12], "Specialist");
            for (int d = 0; d < 7; d++)
            {
                doctor.AddAvailability(clinicId, (DayOfWeek)d, new TimeOnly(0, 0), new TimeOnly(23, 59));
            }
            await db.Doctors.AddAsync(doctor);
            doctorId = doctor.Id;

            var recUser = User.Create(receptionistEmail, "Sara", "Reception", UserRole.Receptionist, tenantId);
            recUser.SetPasswordHash(passwordHasher.HashPassword(password));
            await db.Users.AddAsync(recUser);

            var pUser = User.Create($"p_social_{Guid.NewGuid():N}@clinic.com", "Ali", "Social", UserRole.Patient, tenantId);
            pUser.SetPasswordHash(passwordHasher.HashPassword(password));
            await db.Users.AddAsync(pUser);
            var patient = Patient.Create(tenantId, pUser.Id, new DateOnly(1995, 4, 4), Gender.Male);
            await db.Patients.AddAsync(patient);
            patientId = patient.Id;

            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Slug", _factory.DefaultTenantSlug);
        var auth = await (await client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(receptionistEmail, password))).Content.ReadFromJsonAsync<AuthResponseDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        var tomorrow = DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(1).AddHours(12), DateTimeKind.Utc);

        // 1. Public attributed booking with UTM parameters (Facebook campaign)
        var publicRes = await client.PostAsJsonAsync(
            "/api/v1/public/bookings?utm_source=facebook&utm_medium=social&utm_campaign=dermatology",
            new PublicAttributedBookingRequest(
                tenantId,
                doctorId,
                patientId,
                clinicId,
                tomorrow,
                tomorrow.AddMinutes(30),
                "Booked from Facebook ad"));
        Assert.Equal(HttpStatusCode.OK, publicRes.StatusCode);
        var attributedAppt = await publicRes.Content.ReadFromJsonAsync<AppointmentDto>();
        Assert.NotNull(attributedAppt);
        Assert.Equal(AppointmentSource.Facebook, attributedAppt.Source);
        Assert.Equal("facebook", attributedAppt.UtmSource);
        Assert.Equal("social", attributedAppt.UtmMedium);
        Assert.Equal("dermatology", attributedAppt.UtmCampaign);

        // 2. Receptionist Walk-in registration & immediate check-in
        var walkInSlot = DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(1).AddHours(15), DateTimeKind.Utc);
        var walkInRes = await client.PostAsJsonAsync(
            "/api/v1/reception/walk-ins",
            new RegisterWalkInRequest(
                tenantId,
                doctorId,
                patientId,
                clinicId,
                walkInSlot,
                walkInSlot.AddMinutes(30),
                "Urgent walk-in visit",
                ImmediateCheckIn: true));
        Assert.Equal(HttpStatusCode.OK, walkInRes.StatusCode);
        var walkInResult = await walkInRes.Content.ReadFromJsonAsync<WalkInRegistrationResultDto>();
        Assert.NotNull(walkInResult);
        Assert.Equal(AppointmentSource.WalkIn, walkInResult.Appointment.Source);
        Assert.NotNull(walkInResult.QueueItem);
        Assert.True(walkInResult.QueueItem.QueueNumber >= 1);
    }

    [Fact]
    public async Task TenantIsolation_AuthenticatedUserSpoofsAnotherTenantHeader_ReturnsForbidden()
    {
        _ = _factory.CreateClient();

        string patientEmail = $"spoof_test_{Guid.NewGuid():N}@clinic.com";
        string password = "SpoofPassword123!";
        string otherTenantSlug = $"other-tenant-{Guid.NewGuid():N}"[..20];

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClinicBookingDbContext>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            var defaultTenant = await db.Tenants.IgnoreQueryFilters().FirstAsync(t => t.Identifier == _factory.DefaultTenantSlug);
            var otherTenant = Tenant.Create("Other Clinic Tenant", otherTenantSlug);
            await db.Tenants.AddAsync(otherTenant);

            var user = User.Create(patientEmail, "Spoof", "Tester", UserRole.Patient, defaultTenant.Id);
            user.SetPasswordHash(passwordHasher.HashPassword(password));
            await db.Users.AddAsync(user);

            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Slug", _factory.DefaultTenantSlug);
        var loginRes = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(patientEmail, password));
        Assert.Equal(HttpStatusCode.OK, loginRes.StatusCode);
        var auth = await loginRes.Content.ReadFromJsonAsync<AuthResponseDto>();

        // Attempt to access another tenant by spoofing X-Tenant-Slug with a valid JWT from DefaultTenant
        var spoofClient = _factory.CreateClient();
        spoofClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        spoofClient.DefaultRequestHeaders.Add("X-Tenant-Slug", otherTenantSlug);

        var response = await spoofClient.GetAsync("/api/v1/appointments/my-upcoming");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task WaitlistCancellationRecovery_CancelAppointmentAutomaticallyOffersSlotAndClaimBooksIt()
    {
        _ = _factory.CreateClient();

        Guid tenantId;
        Guid doctorId;
        Guid clinicId;
        Guid patient1Id;
        Guid patient2Id;
        string receptionistEmail = $"rec_waitlist_{Guid.NewGuid():N}@clinic.com";
        string password = "WaitlistPassword123!";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClinicBookingDbContext>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            var tenant = await db.Tenants.IgnoreQueryFilters().FirstAsync(t => t.Identifier == _factory.DefaultTenantSlug);
            tenantId = tenant.Id;
            var clinic = await db.Clinics.IgnoreQueryFilters().FirstAsync(c => c.TenantId == tenantId);
            clinicId = clinic.Id;
            var specialty = await db.Specialties.IgnoreQueryFilters().FirstAsync();

            var docUser = User.Create($"doc_wl_{Guid.NewGuid():N}@clinic.com", "Doctor", "Waitlist", UserRole.Doctor, tenantId);
            docUser.SetPasswordHash(passwordHasher.HashPassword(password));
            await db.Users.AddAsync(docUser);

            var doctor = new Doctor(Guid.NewGuid(), tenantId, docUser.Id, specialty.Id, $"WL-{Guid.NewGuid():N}"[..12], "Specialist");
            for (int d = 0; d < 7; d++)
            {
                doctor.AddAvailability(clinicId, (DayOfWeek)d, new TimeOnly(0, 0), new TimeOnly(23, 59));
            }
            await db.Doctors.AddAsync(doctor);
            doctorId = doctor.Id;

            var recUser = User.Create(receptionistEmail, "Nour", "Reception", UserRole.Receptionist, tenantId);
            recUser.SetPasswordHash(passwordHasher.HashPassword(password));
            await db.Users.AddAsync(recUser);

            var u1 = User.Create($"wl_p1_{Guid.NewGuid():N}@clinic.com", "Patient", "Booked", UserRole.Patient, tenantId);
            u1.SetPasswordHash(passwordHasher.HashPassword(password));
            await db.Users.AddAsync(u1);
            var p1 = Patient.Create(tenantId, u1.Id, new DateOnly(1990, 1, 1), Gender.Male);
            await db.Patients.AddAsync(p1);
            patient1Id = p1.Id;

            var u2 = User.Create($"wl_p2_{Guid.NewGuid():N}@clinic.com", "Patient", "Waiting", UserRole.Patient, tenantId);
            u2.SetPasswordHash(passwordHasher.HashPassword(password));
            await db.Users.AddAsync(u2);
            var p2 = Patient.Create(tenantId, u2.Id, new DateOnly(1992, 2, 2), Gender.Female);
            await db.Patients.AddAsync(p2);
            patient2Id = p2.Id;

            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Slug", _factory.DefaultTenantSlug);
        var auth = await (await client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(receptionistEmail, password))).Content.ReadFromJsonAsync<AuthResponseDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        var targetDate = DateTime.UtcNow.Date.AddDays(3);
        var slotStart = DateTime.SpecifyKind(targetDate.AddHours(13), DateTimeKind.Utc);
        var slotEnd = DateTime.SpecifyKind(targetDate.AddHours(13).AddMinutes(30), DateTimeKind.Utc);

        // 1. Book slot for Patient 1
        var bookRes = await client.PostAsJsonAsync("/api/v1/appointments",
            new BookAppointmentCommand(tenantId, doctorId, patient1Id, clinicId, slotStart, slotEnd, "Initial booking"));
        Assert.Equal(HttpStatusCode.OK, bookRes.StatusCode);
        var appt1 = await bookRes.Content.ReadFromJsonAsync<AppointmentDto>();

        // 2. Add Patient 2 to Waitlist for that doctor and date
        var wlCreateRes = await client.PostAsJsonAsync("/api/v1/waitlist",
            new CreateWaitlistEntryRequest(
                tenantId,
                clinicId,
                doctorId,
                patient2Id,
                DateOnly.FromDateTime(slotStart),
                new TimeOnly(12, 0),
                new TimeOnly(15, 0),
                Priority: 5,
                Notes: "Waiting for cancellation"));
        Assert.Equal(HttpStatusCode.OK, wlCreateRes.StatusCode);
        var wlEntry = await wlCreateRes.Content.ReadFromJsonAsync<WaitlistEntryDto>();
        Assert.NotNull(wlEntry);
        Assert.Equal("Waiting", wlEntry.Status);

        // 3. Cancel Patient 1's appointment -> automatically offers freed slot to Patient 2
        var cancelRes = await client.PostAsJsonAsync($"/api/v1/appointments/{appt1!.Id}/cancel",
            new CancelAppointmentRequest("Patient 1 cancelled"));
        Assert.Equal(HttpStatusCode.OK, cancelRes.StatusCode);

        // 4. Verify Patient 2's waitlist entry is now 'Offered' and claim it to book Patient 2's appointment
        var claimRes = await client.PostAsJsonAsync($"/api/v1/waitlist/{wlEntry.Id}/claim", new ClaimWaitlistSlotRequest());
        Assert.Equal(HttpStatusCode.OK, claimRes.StatusCode);
        var claimResult = await claimRes.Content.ReadFromJsonAsync<WaitlistClaimResultDto>();
        Assert.NotNull(claimResult);
        Assert.Equal("Booked", claimResult.WaitlistEntry.Status);
        Assert.Equal(patient2Id, claimResult.Appointment.PatientId);
        Assert.Equal(slotStart, claimResult.Appointment.StartTimeUtc);
    }
}
