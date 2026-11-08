using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ClinicBooking.Application.Contracts.Services;
using ClinicBooking.Application.Features.Appointments.Commands.BookAppointment;
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
}
