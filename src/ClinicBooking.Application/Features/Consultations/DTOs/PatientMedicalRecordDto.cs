using ClinicBooking.Application.Features.Prescriptions.DTOs;

namespace ClinicBooking.Application.Features.Consultations.DTOs;

public sealed record PatientMedicalRecordDto(
    Guid PatientId,
    string PatientName,
    DateOnly DateOfBirth,
    string Gender,
    string? BloodGroup,
    string? Allergies,
    string? ChronicConditions,
    string? MedicalNotes,
    IReadOnlyList<ConsultationDto> Consultations,
    IReadOnlyList<PatientVitalSignsDto> VitalSignsHistory,
    IReadOnlyList<PrescriptionDto> Prescriptions);
