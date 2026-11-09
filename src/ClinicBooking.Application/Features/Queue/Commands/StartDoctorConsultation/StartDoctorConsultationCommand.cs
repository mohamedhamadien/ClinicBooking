using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Features.Queue.DTOs;
using FluentValidation;
using MediatR;

namespace ClinicBooking.Application.Features.Queue.Commands.StartDoctorConsultation;

public sealed record StartDoctorConsultationCommand(Guid AppointmentId) : IRequest<Result<QueueItemDto>>;

public sealed class StartDoctorConsultationCommandValidator : AbstractValidator<StartDoctorConsultationCommand>
{
    public StartDoctorConsultationCommandValidator()
    {
        RuleFor(x => x.AppointmentId).NotEmpty();
    }
}

public sealed class StartDoctorConsultationCommandHandler : IRequestHandler<StartDoctorConsultationCommand, Result<QueueItemDto>>
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly IDoctorRepository _doctorRepository;
    private readonly IClinicRepository _clinicRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public StartDoctorConsultationCommandHandler(
        IAppointmentRepository appointmentRepository,
        IPatientRepository patientRepository,
        IDoctorRepository doctorRepository,
        IClinicRepository clinicRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _appointmentRepository = appointmentRepository;
        _patientRepository = patientRepository;
        _doctorRepository = doctorRepository;
        _clinicRepository = clinicRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<QueueItemDto>> Handle(StartDoctorConsultationCommand request, CancellationToken cancellationToken)
    {
        var appt = await _appointmentRepository.GetByIdAsync(request.AppointmentId, cancellationToken);
        if (appt is null)
        {
            return Result.Failure<QueueItemDto>(Error.NotFound("Appointment", request.AppointmentId));
        }

        appt.StartConsultation();

        await _appointmentRepository.UpdateAsync(appt, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var patient = await _patientRepository.GetByIdAsync(appt.PatientId, cancellationToken);
        string patientName = "Patient";
        if (patient is not null)
        {
            var user = await _userRepository.GetByIdAsync(patient.UserId, cancellationToken);
            patientName = user?.FullName ?? patientName;
        }

        var doctor = await _doctorRepository.GetByIdAsync(appt.DoctorId, cancellationToken);
        string doctorName = "Doctor";
        if (doctor is not null)
        {
            var user = await _userRepository.GetByIdAsync(doctor.UserId, cancellationToken);
            doctorName = user?.FullName ?? doctorName;
        }

        var clinic = await _clinicRepository.GetByIdAsync(appt.ClinicId, cancellationToken);

        var dto = new QueueItemDto(
            appt.Id,
            appt.QueueNumber,
            appt.PatientId,
            patientName,
            appt.DoctorId,
            doctorName,
            appt.ClinicId,
            clinic?.NameEn ?? "Clinic",
            appt.StartTimeUtc,
            appt.Status);

        return Result.Success(dto);
    }
}
