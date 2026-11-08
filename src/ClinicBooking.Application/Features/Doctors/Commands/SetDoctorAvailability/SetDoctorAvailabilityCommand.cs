using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Domain.Exceptions;
using FluentValidation;
using MediatR;

namespace ClinicBooking.Application.Features.Doctors.Commands.SetDoctorAvailability;

public sealed record SetDoctorAvailabilityCommand(
    Guid DoctorId,
    Guid ClinicId,
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime) : IRequest<Result>;

public sealed class SetDoctorAvailabilityCommandValidator : AbstractValidator<SetDoctorAvailabilityCommand>
{
    public SetDoctorAvailabilityCommandValidator()
    {
        RuleFor(x => x.DoctorId).NotEmpty();
        RuleFor(x => x.ClinicId).NotEmpty();
        RuleFor(x => x.StartTime)
            .Must((cmd, start) => start < cmd.EndTime)
            .WithMessage("StartTime must be earlier than EndTime.");
    }
}

public sealed class SetDoctorAvailabilityCommandHandler : IRequestHandler<SetDoctorAvailabilityCommand, Result>
{
    private readonly IDoctorRepository _doctorRepository;
    private readonly IClinicRepository _clinicRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SetDoctorAvailabilityCommandHandler(
        IDoctorRepository doctorRepository,
        IClinicRepository clinicRepository,
        IUnitOfWork unitOfWork)
    {
        _doctorRepository = doctorRepository;
        _clinicRepository = clinicRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(SetDoctorAvailabilityCommand request, CancellationToken cancellationToken)
    {
        var doctor = await _doctorRepository.GetByIdWithSchedulesAsync(request.DoctorId, cancellationToken);
        if (doctor is null)
        {
            return Result.Failure(Error.NotFound("Doctor", request.DoctorId));
        }

        var clinic = await _clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);
        if (clinic is null || !clinic.IsActive || clinic.TenantId != doctor.TenantId)
        {
            return Result.Failure(Error.NotFound("Clinic", request.ClinicId));
        }

        try
        {
            doctor.AddAvailability(request.ClinicId, request.DayOfWeek, request.StartTime, request.EndTime);
            await _doctorRepository.UpdateAsync(doctor, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (DomainValidationException valEx)
        {
            return Result.Failure(Error.Conflict("Doctor.OverlappingShift", valEx.Message));
        }
    }
}
