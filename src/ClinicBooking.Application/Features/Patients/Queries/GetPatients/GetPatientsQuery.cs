using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Features.Patients.DTOs;
using MediatR;

namespace ClinicBooking.Application.Features.Patients.Queries.GetPatients;

public sealed record GetPatientsQuery(
    Guid TenantId,
    string? SearchTerm = null) : IRequest<Result<IReadOnlyList<PatientSummaryDto>>>;

public sealed class GetPatientsQueryHandler : IRequestHandler<GetPatientsQuery, Result<IReadOnlyList<PatientSummaryDto>>>
{
    private readonly IPatientRepository _patientRepository;
    private readonly IUserRepository _userRepository;

    public GetPatientsQueryHandler(
        IPatientRepository patientRepository,
        IUserRepository userRepository)
    {
        _patientRepository = patientRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<IReadOnlyList<PatientSummaryDto>>> Handle(
        GetPatientsQuery request,
        CancellationToken cancellationToken)
    {
        var patients = await _patientRepository.ListAsync(request.TenantId, request.SearchTerm, cancellationToken);
        var dtos = new List<PatientSummaryDto>();

        foreach (var p in patients)
        {
            var user = await _userRepository.GetByIdAsync(p.UserId, cancellationToken);
            var fullName = user?.FullName ?? "Unknown Patient";
            var email = user?.Email.Value ?? string.Empty;
            var phone = p.EmergencyContact?.Value;

            dtos.Add(new PatientSummaryDto(
                p.Id,
                p.UserId,
                fullName,
                email,
                phone,
                p.DateOfBirth,
                p.Gender.ToString(),
                p.BloodGroup,
                p.Allergies,
                p.ChronicConditions,
                p.EmergencyContact?.Value,
                p.CreatedAtUtc));
        }

        return Result.Success<IReadOnlyList<PatientSummaryDto>>(dtos);
    }
}
