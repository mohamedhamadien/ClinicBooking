using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using FluentValidation;
using MediatR;

namespace ClinicBooking.Application.Features.Doctors.Commands.RemoveAvailabilityException;

public sealed record RemoveAvailabilityExceptionCommand(
    Guid DoctorId,
    Guid ExceptionId) : IRequest<Result>;

public sealed class RemoveAvailabilityExceptionCommandValidator : AbstractValidator<RemoveAvailabilityExceptionCommand>
{
    public RemoveAvailabilityExceptionCommandValidator()
    {
        RuleFor(x => x.DoctorId).NotEmpty();
        RuleFor(x => x.ExceptionId).NotEmpty();
    }
}

public sealed class RemoveAvailabilityExceptionCommandHandler : IRequestHandler<RemoveAvailabilityExceptionCommand, Result>
{
    private readonly IDoctorRepository _doctorRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveAvailabilityExceptionCommandHandler(IDoctorRepository doctorRepository, IUnitOfWork unitOfWork)
    {
        _doctorRepository = doctorRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RemoveAvailabilityExceptionCommand request, CancellationToken cancellationToken)
    {
        var doctor = await _doctorRepository.GetByIdWithSchedulesAsync(request.DoctorId, cancellationToken);
        if (doctor is null)
        {
            return Result.Failure(Error.NotFound("Doctor", request.DoctorId));
        }

        doctor.RemoveBlackoutException(request.ExceptionId);
        await _doctorRepository.UpdateAsync(doctor, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
