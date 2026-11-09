namespace ClinicBooking.Application.Features.Prescriptions.DTOs;

public sealed record PrescriptionItemDto(
    Guid Id,
    string MedicationName,
    string Dosage,
    string Frequency,
    string Duration,
    string? Instructions);

public sealed record PrescriptionDto(
    Guid Id,
    Guid TenantId,
    Guid? ConsultationId,
    Guid PatientId,
    Guid DoctorId,
    Guid ClinicId,
    string PrescriptionNumber,
    string Diagnosis,
    string? GeneralInstructions,
    DateTime IssuedAtUtc,
    IReadOnlyList<PrescriptionItemDto> Items,
    string? DoctorName = null,
    string? PatientName = null,
    string? ClinicName = null);
