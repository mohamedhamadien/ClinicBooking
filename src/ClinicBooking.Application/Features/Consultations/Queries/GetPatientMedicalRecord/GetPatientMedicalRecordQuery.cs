using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Features.Consultations.DTOs;
using ClinicBooking.Application.Features.Prescriptions.DTOs;
using MediatR;

namespace ClinicBooking.Application.Features.Consultations.Queries.GetPatientMedicalRecord;

public sealed record GetPatientMedicalRecordQuery(Guid PatientId, Guid TenantId) : IRequest<Result<PatientMedicalRecordDto>>;

public sealed class GetPatientMedicalRecordQueryHandler : IRequestHandler<GetPatientMedicalRecordQuery, Result<PatientMedicalRecordDto>>
{
    private readonly IPatientRepository _patientRepository;
    private readonly IUserRepository _userRepository;
    private readonly IConsultationRepository _consultationRepository;
    private readonly IPatientVitalSignsRepository _vitalSignsRepository;
    private readonly IPrescriptionRepository _prescriptionRepository;
    private readonly IDoctorRepository _doctorRepository;
    private readonly IClinicRepository _clinicRepository;

    public GetPatientMedicalRecordQueryHandler(
        IPatientRepository patientRepository,
        IUserRepository userRepository,
        IConsultationRepository consultationRepository,
        IPatientVitalSignsRepository vitalSignsRepository,
        IPrescriptionRepository prescriptionRepository,
        IDoctorRepository doctorRepository,
        IClinicRepository clinicRepository)
    {
        _patientRepository = patientRepository;
        _userRepository = userRepository;
        _consultationRepository = consultationRepository;
        _vitalSignsRepository = vitalSignsRepository;
        _prescriptionRepository = prescriptionRepository;
        _doctorRepository = doctorRepository;
        _clinicRepository = clinicRepository;
    }

    public async Task<Result<PatientMedicalRecordDto>> Handle(GetPatientMedicalRecordQuery request, CancellationToken cancellationToken)
    {
        var patient = await _patientRepository.GetByIdAsync(request.PatientId, cancellationToken);
        if (patient is null)
        {
            return Result.Failure<PatientMedicalRecordDto>(Error.NotFound("Patient", request.PatientId));
        }

        var user = await _userRepository.GetByIdAsync(patient.UserId, cancellationToken);
        var patientName = user?.FullName ?? "Patient";

        var consultations = await _consultationRepository.GetByPatientIdAsync(request.TenantId, request.PatientId, cancellationToken);
        var vitals = await _vitalSignsRepository.GetByPatientIdAsync(request.TenantId, request.PatientId, cancellationToken);
        var prescriptions = await _prescriptionRepository.GetByPatientIdAsync(request.TenantId, request.PatientId, cancellationToken);

        var doctors = await _doctorRepository.ListAsync(request.TenantId, null, null, null, cancellationToken);
        var clinics = await _clinicRepository.ListAsync(request.TenantId, cancellationToken);

        var clinicMap = clinics.ToDictionary(c => c.Id, c => c.NameEn);

        var consultationDtos = consultations.Select(c =>
        {
            clinicMap.TryGetValue(c.ClinicId, out var clinicName);
            return new ConsultationDto(
                c.Id,
                c.AppointmentId,
                c.PatientId,
                c.DoctorId,
                c.ClinicId,
                c.ChiefComplaint,
                c.ExaminationNotes,
                c.Diagnosis,
                c.TreatmentPlan,
                c.ConsultationDateUtc,
                null,
                null,
                patientName,
                clinicName,
                c.FollowUpDateUtc,
                c.LabRequests);
        }).ToList();

        var vitalDtos = vitals.Select(v => new PatientVitalSignsDto(
            v.Id,
            v.PatientId,
            v.ConsultationId,
            v.SystolicBp,
            v.DiastolicBp,
            v.HeartRate,
            v.TemperatureCelsius,
            v.WeightKg,
            v.HeightCm,
            v.BloodGlucoseMgDl,
            v.Bmi,
            v.RecordedAtUtc)).ToList();

        var prescriptionDtos = prescriptions.Select(p =>
        {
            clinicMap.TryGetValue(p.ClinicId, out var clinicName);
            return new PrescriptionDto(
                p.Id,
                p.TenantId,
                p.ConsultationId,
                p.PatientId,
                p.DoctorId,
                p.ClinicId,
                p.PrescriptionNumber,
                p.Diagnosis,
                p.GeneralInstructions,
                p.IssuedAtUtc,
                p.Items.Select(i => new PrescriptionItemDto(i.Id, i.MedicationName, i.Dosage, i.Frequency, i.Duration, i.Instructions)).ToList(),
                null,
                patientName,
                clinicName);
        }).ToList();

        var recordDto = new PatientMedicalRecordDto(
            patient.Id,
            patientName,
            patient.DateOfBirth,
            patient.Gender.ToString(),
            patient.BloodGroup,
            patient.Allergies,
            patient.ChronicConditions,
            patient.MedicalNotes,
            consultationDtos,
            vitalDtos,
            prescriptionDtos);

        return Result.Success(recordDto);
    }
}
