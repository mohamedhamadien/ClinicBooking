using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Features.Consultations.DTOs;
using ClinicBooking.Domain.Entities;
using FluentValidation;
using MediatR;

namespace ClinicBooking.Application.Features.Consultations.Commands.CreateConsultation;

public sealed record VitalSignsInput(
    int? SystolicBp = null,
    int? DiastolicBp = null,
    int? HeartRate = null,
    decimal? TemperatureCelsius = null,
    decimal? WeightKg = null,
    decimal? HeightCm = null,
    decimal? BloodGlucoseMgDl = null);

public sealed record CreateConsultationCommand(
    Guid TenantId,
    Guid AppointmentId,
    Guid PatientId,
    Guid DoctorId,
    Guid ClinicId,
    string ChiefComplaint,
    string ExaminationNotes,
    string Diagnosis,
    string TreatmentPlan,
    VitalSignsInput? VitalSigns = null) : IRequest<Result<ConsultationDto>>;

public sealed class CreateConsultationCommandValidator : AbstractValidator<CreateConsultationCommand>
{
    public CreateConsultationCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.AppointmentId).NotEmpty();
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.DoctorId).NotEmpty();
        RuleFor(x => x.Diagnosis).NotEmpty().WithMessage("Diagnosis is required.").MaximumLength(1000);
        RuleFor(x => x.ChiefComplaint).MaximumLength(1000);
        RuleFor(x => x.ExaminationNotes).MaximumLength(3000);
        RuleFor(x => x.TreatmentPlan).MaximumLength(3000);
    }
}

public sealed class CreateConsultationCommandHandler : IRequestHandler<CreateConsultationCommand, Result<ConsultationDto>>
{
    private readonly IConsultationRepository _consultationRepository;
    private readonly IPatientVitalSignsRepository _vitalSignsRepository;
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateConsultationCommandHandler(
        IConsultationRepository consultationRepository,
        IPatientVitalSignsRepository vitalSignsRepository,
        IAppointmentRepository appointmentRepository,
        IUnitOfWork unitOfWork)
    {
        _consultationRepository = consultationRepository;
        _vitalSignsRepository = vitalSignsRepository;
        _appointmentRepository = appointmentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ConsultationDto>> Handle(CreateConsultationCommand request, CancellationToken cancellationToken)
    {
        var existingNote = await _consultationRepository.GetByAppointmentIdAsync(request.AppointmentId, cancellationToken);
        if (existingNote is not null)
        {
            return Result.Failure<ConsultationDto>(Error.Conflict("Consultation.AlreadyExists", "A consultation note has already been recorded for this appointment."));
        }

        var appointment = await _appointmentRepository.GetByIdAsync(request.AppointmentId, cancellationToken);
        if (appointment is null)
        {
            return Result.Failure<ConsultationDto>(Error.NotFound("Appointment", request.AppointmentId));
        }

        var note = ConsultationNote.Create(
            request.TenantId,
            request.AppointmentId,
            request.PatientId,
            request.DoctorId,
            request.ClinicId,
            request.ChiefComplaint,
            request.ExaminationNotes,
            request.Diagnosis,
            request.TreatmentPlan,
            DateTime.UtcNow);

        await _consultationRepository.AddAsync(note, cancellationToken);

        PatientVitalSignsDto? vitalsDto = null;
        if (request.VitalSigns is not null)
        {
            var vs = PatientVitalSigns.Create(
                request.TenantId,
                request.PatientId,
                note.Id,
                request.VitalSigns.SystolicBp,
                request.VitalSigns.DiastolicBp,
                request.VitalSigns.HeartRate,
                request.VitalSigns.TemperatureCelsius,
                request.VitalSigns.WeightKg,
                request.VitalSigns.HeightCm,
                request.VitalSigns.BloodGlucoseMgDl,
                DateTime.UtcNow);

            await _vitalSignsRepository.AddAsync(vs, cancellationToken);

            vitalsDto = new PatientVitalSignsDto(
                vs.Id,
                vs.PatientId,
                vs.ConsultationId,
                vs.SystolicBp,
                vs.DiastolicBp,
                vs.HeartRate,
                vs.TemperatureCelsius,
                vs.WeightKg,
                vs.HeightCm,
                vs.BloodGlucoseMgDl,
                vs.Bmi,
                vs.RecordedAtUtc);
        }

        // Complete the appointment
        try
        {
            appointment.Complete();
        }
        catch
        {
            // If already completed or not transitioning, ignore error to ensure consultation saves
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new ConsultationDto(
            note.Id,
            note.AppointmentId,
            note.PatientId,
            note.DoctorId,
            note.ClinicId,
            note.ChiefComplaint,
            note.ExaminationNotes,
            note.Diagnosis,
            note.TreatmentPlan,
            note.ConsultationDateUtc,
            vitalsDto);

        return Result.Success(dto);
    }
}
