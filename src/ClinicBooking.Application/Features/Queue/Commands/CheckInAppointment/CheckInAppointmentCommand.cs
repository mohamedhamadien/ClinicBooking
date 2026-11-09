using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Features.Queue.DTOs;
using ClinicBooking.Domain.Entities;
using FluentValidation;
using MediatR;

namespace ClinicBooking.Application.Features.Queue.Commands.CheckInAppointment;

public sealed record CheckInAppointmentCommand(Guid AppointmentId) : IRequest<Result<QueueItemDto>>;

public sealed class CheckInAppointmentCommandValidator : AbstractValidator<CheckInAppointmentCommand>
{
    public CheckInAppointmentCommandValidator()
    {
        RuleFor(x => x.AppointmentId).NotEmpty();
    }
}

public sealed class CheckInAppointmentCommandHandler : IRequestHandler<CheckInAppointmentCommand, Result<QueueItemDto>>
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly IDoctorRepository _doctorRepository;
    private readonly IClinicRepository _clinicRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CheckInAppointmentCommandHandler(
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

    public async Task<Result<QueueItemDto>> Handle(CheckInAppointmentCommand request, CancellationToken cancellationToken)
    {
        var appt = await _appointmentRepository.GetByIdAsync(request.AppointmentId, cancellationToken);
        if (appt is null)
        {
            return Result.Failure<QueueItemDto>(Error.NotFound("Appointment", request.AppointmentId));
        }

        // Determine next queue token for today at this clinic
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        var existingForDay = await _appointmentRepository.GetAllAppointmentsAsync(
            appt.TenantId,
            today,
            tomorrow,
            null,
            appt.ClinicId,
            cancellationToken);

        var maxQueue = existingForDay
            .Where(a => a.QueueNumber.HasValue)
            .Select(a => a.QueueNumber!.Value)
            .DefaultIfEmpty(0)
            .Max();

        var queueToken = maxQueue + 1;
        appt.CheckIn(queueToken);

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
