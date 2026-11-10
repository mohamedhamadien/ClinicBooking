namespace ClinicBooking.Application.Features.Consultations.DTOs;

public sealed record PatientVitalSignsDto(
    Guid Id,
    Guid PatientId,
    Guid? ConsultationId,
    int? SystolicBp,
    int? DiastolicBp,
    int? HeartRate,
    decimal? TemperatureCelsius,
    decimal? WeightKg,
    decimal? HeightCm,
    decimal? BloodGlucoseMgDl,
    decimal? Bmi,
    DateTime RecordedAtUtc);

public sealed record ConsultationDto(
    Guid Id,
    Guid AppointmentId,
    Guid PatientId,
    Guid DoctorId,
    Guid ClinicId,
    string ChiefComplaint,
    string ExaminationNotes,
    string Diagnosis,
    string TreatmentPlan,
    DateTime ConsultationDateUtc,
    PatientVitalSignsDto? VitalSigns = null,
    string? DoctorName = null,
    string? PatientName = null,
    string? ClinicName = null,
    DateTime? FollowUpDateUtc = null,
    string? LabRequests = null);
