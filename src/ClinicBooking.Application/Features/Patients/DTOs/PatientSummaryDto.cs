namespace ClinicBooking.Application.Features.Patients.DTOs;

public sealed record PatientSummaryDto(
    Guid Id,
    Guid UserId,
    string FullName,
    string Email,
    string? PhoneNumber,
    DateOnly DateOfBirth,
    string Gender,
    string? BloodGroup,
    string? Allergies,
    string? ChronicConditions,
    string? EmergencyContact,
    DateTime CreatedAtUtc);
