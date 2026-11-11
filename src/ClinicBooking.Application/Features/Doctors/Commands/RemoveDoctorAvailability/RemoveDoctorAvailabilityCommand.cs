using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using FluentValidation;
using MediatR;

namespace ClinicBooking.Application.Features.Doctors.Commands.RemoveDoctorAvailability;

public sealed record RemoveDoctorAvailabilityCommand(
    Guid DoctorId,
    Guid AvailabilityId) : IRequest<Result>;

public sealed class RemoveDoctorAvailabilityCommandValidator : AbstractValidator<RemoveDoctorAvailabilityCommand>
{
    public RemoveDoctorAvailabilityCommandValidator()
    {
        RuleFor(x => x.DoctorId).NotEmpty();
        RuleFor(x => x.AvailabilityId).NotEmpty();
    }
}

public sealed class RemoveDoctorAvailabilityCommandHandler : IRequestHandler<RemoveDoctorAvailabilityCommand, Result>
{
    private readonly IDoctorRepository _doctorRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveDoctorAvailabilityCommandHandler(IDoctorRepository doctorRepository, IUnitOfWork unitOfWork)
    {
        _doctorRepository = doctorRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RemoveDoctorAvailabilityCommand request, CancellationToken cancellationToken)
    {
        var doctor = await _doctorRepository.GetByIdWithSchedulesAsync(request.DoctorId, cancellationToken);
        if (doctor is null)
        {
            return Result.Failure(Error.NotFound("Doctor", request.DoctorId));
        }

        doctor.RemoveAvailability(request.AvailabilityId);
        await _doctorRepository.UpdateAsync(doctor, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
