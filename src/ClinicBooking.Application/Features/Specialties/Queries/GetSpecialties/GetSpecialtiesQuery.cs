using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Contracts.Services;
using ClinicBooking.Application.Features.Specialties.DTOs;
using MediatR;

namespace ClinicBooking.Application.Features.Specialties.Queries.GetSpecialties;

public sealed record GetSpecialtiesQuery(Guid? TenantId = null) : IRequest<Result<IReadOnlyList<SpecialtyDto>>>;

public sealed class GetSpecialtiesQueryHandler : IRequestHandler<GetSpecialtiesQuery, Result<IReadOnlyList<SpecialtyDto>>>
{
    private readonly ISpecialtyRepository _specialtyRepository;
    private readonly ILocalizationService _localizationService;

    public GetSpecialtiesQueryHandler(
        ISpecialtyRepository specialtyRepository,
        ILocalizationService localizationService)
    {
        _specialtyRepository = specialtyRepository;
        _localizationService = localizationService;
    }

    public async Task<Result<IReadOnlyList<SpecialtyDto>>> Handle(GetSpecialtiesQuery request, CancellationToken cancellationToken)
    {
        var specialties = await _specialtyRepository.ListAsync(request.TenantId, cancellationToken);
        var isArabic = _localizationService.IsArabic;

        var dtos = specialties.Select(s => new SpecialtyDto(
            s.Id,
            s.TenantId,
            s.NameEn,
            s.DescriptionEn,
            s.IsActive,
            s.NameAr,
            s.DescriptionAr,
            s.GetLocalizedName(isArabic),
            s.GetLocalizedDescription(isArabic))).ToList();

        return Result.Success<IReadOnlyList<SpecialtyDto>>(dtos);
    }
}
