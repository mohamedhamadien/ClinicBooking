using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Features.Clinics.DTOs;
using MediatR;

namespace ClinicBooking.Application.Features.Clinics.Queries.GetClinics;

public sealed record GetClinicsQuery(Guid TenantId) : IRequest<Result<IReadOnlyList<ClinicDto>>>;

public sealed class GetClinicsQueryHandler : IRequestHandler<GetClinicsQuery, Result<IReadOnlyList<ClinicDto>>>
{
    private readonly IClinicRepository _clinicRepository;

    public GetClinicsQueryHandler(IClinicRepository clinicRepository)
    {
        _clinicRepository = clinicRepository;
    }

    public async Task<Result<IReadOnlyList<ClinicDto>>> Handle(GetClinicsQuery request, CancellationToken cancellationToken)
    {
        var clinics = await _clinicRepository.ListAsync(request.TenantId, cancellationToken);

        var dtos = clinics.Select(c => new ClinicDto(
            c.Id,
            c.TenantId,
            c.Name,
            c.Address,
            c.City,
            c.PhoneNumber.Value,
            c.IsActive)).ToList();

        return Result.Success<IReadOnlyList<ClinicDto>>(dtos);
    }
}
