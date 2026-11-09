using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Features.Prescriptions.DTOs;
using MediatR;

namespace ClinicBooking.Application.Features.Prescriptions.Queries.GetPatientPrescriptions;

public sealed record GetPatientPrescriptionsQuery(Guid TenantId, Guid PatientId) : IRequest<Result<IReadOnlyList<PrescriptionDto>>>;

public sealed class GetPatientPrescriptionsQueryHandler : IRequestHandler<GetPatientPrescriptionsQuery, Result<IReadOnlyList<PrescriptionDto>>>
{
    private readonly IPrescriptionRepository _prescriptionRepository;
    private readonly IClinicRepository _clinicRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly IUserRepository _userRepository;

    public GetPatientPrescriptionsQueryHandler(
        IPrescriptionRepository prescriptionRepository,
        IClinicRepository clinicRepository,
        IPatientRepository patientRepository,
        IUserRepository userRepository)
    {
        _prescriptionRepository = prescriptionRepository;
        _clinicRepository = clinicRepository;
        _patientRepository = patientRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<IReadOnlyList<PrescriptionDto>>> Handle(GetPatientPrescriptionsQuery request, CancellationToken cancellationToken)
    {
        var prescriptions = await _prescriptionRepository.GetByPatientIdAsync(request.TenantId, request.PatientId, cancellationToken);
        var clinics = await _clinicRepository.ListAsync(request.TenantId, cancellationToken);
        var clinicMap = clinics.ToDictionary(c => c.Id, c => c.NameEn);

        var patient = await _patientRepository.GetByIdAsync(request.PatientId, cancellationToken);
        string? patientName = null;
        if (patient is not null)
        {
            var user = await _userRepository.GetByIdAsync(patient.UserId, cancellationToken);
            patientName = user?.FullName;
        }

        var dtos = prescriptions.Select(p =>
        {
            clinicMap.TryGetValue(p.ClinicId, out var clinicName);
            return new PrescriptionDto(
                p.Id,
                p.TenantId,
                p.ConsultationId,
                p.PatientId,
                p.DoctorId,
                p.ClinicId,
                p.PrescriptionNumber,
                p.Diagnosis,
                p.GeneralInstructions,
                p.IssuedAtUtc,
                p.Items.Select(i => new PrescriptionItemDto(i.Id, i.MedicationName, i.Dosage, i.Frequency, i.Duration, i.Instructions)).ToList(),
                null,
                patientName,
                clinicName);
        }).ToList();

        return Result.Success<IReadOnlyList<PrescriptionDto>>(dtos);
    }
}
