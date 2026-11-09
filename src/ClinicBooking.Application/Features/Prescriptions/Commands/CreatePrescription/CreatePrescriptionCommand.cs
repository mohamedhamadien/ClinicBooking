using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Features.Prescriptions.DTOs;
using ClinicBooking.Domain.Entities;
using FluentValidation;
using MediatR;

namespace ClinicBooking.Application.Features.Prescriptions.Commands.CreatePrescription;

public sealed record PrescriptionItemInput(
    string MedicationName,
    string Dosage,
    string Frequency,
    string Duration,
    string? Instructions = null);

public sealed record CreatePrescriptionCommand(
    Guid TenantId,
    Guid? ConsultationId,
    Guid PatientId,
    Guid DoctorId,
    Guid ClinicId,
    string Diagnosis,
    string? GeneralInstructions = null,
    IReadOnlyList<PrescriptionItemInput>? Items = null) : IRequest<Result<PrescriptionDto>>;

public sealed class CreatePrescriptionCommandValidator : AbstractValidator<CreatePrescriptionCommand>
{
    public CreatePrescriptionCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.DoctorId).NotEmpty();
        RuleFor(x => x.ClinicId).NotEmpty();
        RuleFor(x => x.Diagnosis).NotEmpty().MaximumLength(1000);
    }
}

public sealed class CreatePrescriptionCommandHandler : IRequestHandler<CreatePrescriptionCommand, Result<PrescriptionDto>>
{
    private readonly IPrescriptionRepository _prescriptionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreatePrescriptionCommandHandler(
        IPrescriptionRepository prescriptionRepository,
        IUnitOfWork unitOfWork)
    {
        _prescriptionRepository = prescriptionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PrescriptionDto>> Handle(CreatePrescriptionCommand request, CancellationToken cancellationToken)
    {
        var prescription = Prescription.Create(
            request.TenantId,
            request.ConsultationId,
            request.PatientId,
            request.DoctorId,
            request.ClinicId,
            request.Diagnosis,
            request.GeneralInstructions);

        if (request.Items is not null)
        {
            foreach (var item in request.Items)
            {
                prescription.AddItem(
                    item.MedicationName,
                    item.Dosage,
                    item.Frequency,
                    item.Duration,
                    item.Instructions);
            }
        }

        await _prescriptionRepository.AddAsync(prescription, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new PrescriptionDto(
            prescription.Id,
            prescription.TenantId,
            prescription.ConsultationId,
            prescription.PatientId,
            prescription.DoctorId,
            prescription.ClinicId,
            prescription.PrescriptionNumber,
            prescription.Diagnosis,
            prescription.GeneralInstructions,
            prescription.IssuedAtUtc,
            prescription.Items.Select(i => new PrescriptionItemDto(i.Id, i.MedicationName, i.Dosage, i.Frequency, i.Duration, i.Instructions)).ToList());

        return Result.Success(dto);
    }
}
