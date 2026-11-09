using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using FluentValidation;
using MediatR;

namespace ClinicBooking.Application.Features.Consultations.Commands.UpdatePatientMedicalHistory;

public sealed record UpdatePatientMedicalHistoryCommand(
    Guid PatientId,
    string? BloodGroup = null,
    string? Allergies = null,
    string? ChronicConditions = null,
    string? MedicalNotes = null) : IRequest<Result>;

public sealed class UpdatePatientMedicalHistoryCommandValidator : AbstractValidator<UpdatePatientMedicalHistoryCommand>
{
    public UpdatePatientMedicalHistoryCommandValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.BloodGroup).MaximumLength(10);
        RuleFor(x => x.Allergies).MaximumLength(500);
        RuleFor(x => x.ChronicConditions).MaximumLength(500);
        RuleFor(x => x.MedicalNotes).MaximumLength(1000);
    }
}

public sealed class UpdatePatientMedicalHistoryCommandHandler : IRequestHandler<UpdatePatientMedicalHistoryCommand, Result>
{
    private readonly IPatientRepository _patientRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdatePatientMedicalHistoryCommandHandler(
        IPatientRepository patientRepository,
        IUnitOfWork unitOfWork)
    {
        _patientRepository = patientRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdatePatientMedicalHistoryCommand request, CancellationToken cancellationToken)
    {
        var patient = await _patientRepository.GetByIdAsync(request.PatientId, cancellationToken);
        if (patient is null)
        {
            return Result.Failure(Error.NotFound("Patient", request.PatientId));
        }

        patient.UpdateMedicalHistory(
            request.BloodGroup,
            request.Allergies,
            request.ChronicConditions,
            request.MedicalNotes);

        await _patientRepository.UpdateAsync(patient, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
