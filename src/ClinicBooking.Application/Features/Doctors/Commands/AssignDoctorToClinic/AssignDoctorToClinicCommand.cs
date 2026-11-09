using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using FluentValidation;
using MediatR;

namespace ClinicBooking.Application.Features.Doctors.Commands.AssignDoctorToClinic;

public sealed record AssignDoctorToClinicCommand(
    Guid DoctorId,
    Guid ClinicId,
    decimal ConsultationFee,
    bool IsPrimary = false) : IRequest<Result>;

public sealed class AssignDoctorToClinicCommandValidator : AbstractValidator<AssignDoctorToClinicCommand>
{
    public AssignDoctorToClinicCommandValidator()
    {
        RuleFor(x => x.DoctorId).NotEmpty();
        RuleFor(x => x.ClinicId).NotEmpty();
        RuleFor(x => x.ConsultationFee).GreaterThanOrEqualTo(0);
    }
}

public sealed class AssignDoctorToClinicCommandHandler : IRequestHandler<AssignDoctorToClinicCommand, Result>
{
    private readonly IDoctorRepository _doctorRepository;
    private readonly IClinicRepository _clinicRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AssignDoctorToClinicCommandHandler(
        IDoctorRepository doctorRepository,
        IClinicRepository clinicRepository,
        IUnitOfWork unitOfWork)
    {
        _doctorRepository = doctorRepository;
        _clinicRepository = clinicRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(AssignDoctorToClinicCommand request, CancellationToken cancellationToken)
    {
        var doctor = await _doctorRepository.GetByIdWithSchedulesAsync(request.DoctorId, cancellationToken);
        if (doctor is null)
        {
            return Result.Failure(Error.NotFound("Doctor", request.DoctorId));
        }

        var clinic = await _clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);
        if (clinic is null)
        {
            return Result.Failure(Error.NotFound("Clinic", request.ClinicId));
        }

        doctor.AssignToClinic(request.ClinicId, request.ConsultationFee, request.IsPrimary);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
