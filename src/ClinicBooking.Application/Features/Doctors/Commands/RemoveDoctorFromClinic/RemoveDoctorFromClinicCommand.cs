using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using FluentValidation;
using MediatR;

namespace ClinicBooking.Application.Features.Doctors.Commands.RemoveDoctorFromClinic;

public sealed record RemoveDoctorFromClinicCommand(
    Guid DoctorId,
    Guid ClinicId) : IRequest<Result>;

public sealed class RemoveDoctorFromClinicCommandValidator : AbstractValidator<RemoveDoctorFromClinicCommand>
{
    public RemoveDoctorFromClinicCommandValidator()
    {
        RuleFor(x => x.DoctorId).NotEmpty();
        RuleFor(x => x.ClinicId).NotEmpty();
    }
}

public sealed class RemoveDoctorFromClinicCommandHandler : IRequestHandler<RemoveDoctorFromClinicCommand, Result>
{
    private readonly IDoctorRepository _doctorRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveDoctorFromClinicCommandHandler(
        IDoctorRepository doctorRepository,
        IUnitOfWork unitOfWork)
    {
        _doctorRepository = doctorRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RemoveDoctorFromClinicCommand request, CancellationToken cancellationToken)
    {
        var doctor = await _doctorRepository.GetByIdWithSchedulesAsync(request.DoctorId, cancellationToken);
        if (doctor is null)
        {
            return Result.Failure(Error.NotFound("Doctor", request.DoctorId));
        }

        doctor.RemoveFromClinic(request.ClinicId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
