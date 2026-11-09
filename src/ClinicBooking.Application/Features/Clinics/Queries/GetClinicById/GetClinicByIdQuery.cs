using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Contracts.Services;
using ClinicBooking.Application.Features.Clinics.DTOs;
using MediatR;

namespace ClinicBooking.Application.Features.Clinics.Queries.GetClinicById;

public sealed record GetClinicByIdQuery(Guid Id) : IRequest<Result<ClinicDto>>;

public sealed class GetClinicByIdQueryHandler : IRequestHandler<GetClinicByIdQuery, Result<ClinicDto>>
{
    private readonly IClinicRepository _clinicRepository;
    private readonly ILocalizationService _localizationService;

    public GetClinicByIdQueryHandler(
        IClinicRepository clinicRepository,
        ILocalizationService localizationService)
    {
        _clinicRepository = clinicRepository;
        _localizationService = localizationService;
    }

    public async Task<Result<ClinicDto>> Handle(GetClinicByIdQuery request, CancellationToken cancellationToken)
    {
        var clinic = await _clinicRepository.GetByIdAsync(request.Id, cancellationToken);
        if (clinic is null)
        {
            return Result.Failure<ClinicDto>(Error.NotFound("Clinic", request.Id));
        }

        var isArabic = _localizationService.IsArabic;

        var dto = new ClinicDto(
            clinic.Id,
            clinic.TenantId,
            clinic.GetLocalizedName(isArabic),
            clinic.GetLocalizedAddress(isArabic),
            clinic.GetLocalizedCity(isArabic),
            clinic.PhoneNumber.Value,
            clinic.IsActive,
            clinic.NameAr,
            clinic.AddressAr,
            clinic.CityAr);

        return Result.Success(dto);
    }
}
