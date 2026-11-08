using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Domain.Exceptions;
using FluentValidation;
using MediatR;

namespace ClinicBooking.Application.Features.Doctors.Commands.AddAvailabilityException;

public sealed record AddAvailabilityExceptionCommand(
    Guid DoctorId,
    DateTime StartUtc,
    DateTime EndUtc,
    string Reason) : IRequest<Result>;

public sealed class AddAvailabilityExceptionCommandValidator : AbstractValidator<AddAvailabilityExceptionCommand>
{
    public AddAvailabilityExceptionCommandValidator()
    {
        RuleFor(x => x.DoctorId).NotEmpty();
        RuleFor(x => x.StartUtc)
            .Must((cmd, start) => start < cmd.EndUtc)
            .WithMessage("StartUtc must be earlier than EndUtc.");
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(200);
    }
}

public sealed class AddAvailabilityExceptionCommandHandler : IRequestHandler<AddAvailabilityExceptionCommand, Result>
{
    private readonly IDoctorRepository _doctorRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddAvailabilityExceptionCommandHandler(IDoctorRepository doctorRepository, IUnitOfWork unitOfWork)
    {
        _doctorRepository = doctorRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(AddAvailabilityExceptionCommand request, CancellationToken cancellationToken)
    {
        var doctor = await _doctorRepository.GetByIdWithSchedulesAsync(request.DoctorId, cancellationToken);
        if (doctor is null)
        {
            return Result.Failure(Error.NotFound("Doctor", request.DoctorId));
        }

        try
        {
            doctor.AddBlackoutException(request.StartUtc, request.EndUtc, request.Reason);
            await _doctorRepository.UpdateAsync(doctor, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (DomainValidationException valEx)
        {
            return Result.Failure(Error.Validation("Doctor.InvalidBlackout", valEx.Message));
        }
    }
}
