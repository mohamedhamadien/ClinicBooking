using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Features.Specialties.DTOs;
using MediatR;

namespace ClinicBooking.Application.Features.Specialties.Queries.GetSpecialties;

public sealed record GetSpecialtiesQuery(Guid? TenantId = null) : IRequest<Result<IReadOnlyList<SpecialtyDto>>>;

public sealed class GetSpecialtiesQueryHandler : IRequestHandler<GetSpecialtiesQuery, Result<IReadOnlyList<SpecialtyDto>>>
{
    private readonly ISpecialtyRepository _specialtyRepository;

    public GetSpecialtiesQueryHandler(ISpecialtyRepository specialtyRepository)
    {
        _specialtyRepository = specialtyRepository;
    }

    public async Task<Result<IReadOnlyList<SpecialtyDto>>> Handle(GetSpecialtiesQuery request, CancellationToken cancellationToken)
    {
        var specialties = await _specialtyRepository.ListAsync(request.TenantId, cancellationToken);

        var dtos = specialties.Select(s => new SpecialtyDto(
            s.Id,
            s.TenantId,
            s.Name,
            s.Description,
            s.IsActive)).ToList();

        return Result.Success<IReadOnlyList<SpecialtyDto>>(dtos);
    }
}
