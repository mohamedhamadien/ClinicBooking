using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Contracts.Services;
using ClinicBooking.Application.Features.Clinics.DTOs;
using MediatR;

namespace ClinicBooking.Application.Features.Clinics.Queries.GetClinics;

public sealed record GetClinicsQuery(Guid TenantId) : IRequest<Result<IReadOnlyList<ClinicDto>>>;

public sealed class GetClinicsQueryHandler : IRequestHandler<GetClinicsQuery, Result<IReadOnlyList<ClinicDto>>>
{
    private readonly IClinicRepository _clinicRepository;
    private readonly ILocalizationService _localizationService;

    public GetClinicsQueryHandler(
        IClinicRepository clinicRepository,
        ILocalizationService localizationService)
    {
        _clinicRepository = clinicRepository;
        _localizationService = localizationService;
    }

    public async Task<Result<IReadOnlyList<ClinicDto>>> Handle(GetClinicsQuery request, CancellationToken cancellationToken)
    {
        var clinics = await _clinicRepository.ListAsync(request.TenantId, cancellationToken);
        var isArabic = _localizationService.IsArabic;

        var dtos = clinics.Select(c => new ClinicDto(
            c.Id,
            c.TenantId,
            c.NameEn,
            c.AddressEn,
            c.CityEn,
            c.PhoneNumber.Value,
            c.IsActive,
            c.NameAr,
            c.AddressAr,
            c.CityAr,
            c.GetLocalizedName(isArabic),
            c.GetLocalizedAddress(isArabic),
            c.GetLocalizedCity(isArabic),
            c.SubscriptionStartDateUtc,
            c.SubscriptionEndDateUtc,
            c.SubscriptionPlan ?? "Enterprise",
            c.IsSubscriptionActive,
            c.SubscriptionDaysRemaining,
            c.IsPrivateClinic,
            c.IsSingleDoctorClinic,
            c.EnableProceduresDuringShifts,
            c.EnableGeneralClinicFeatures,
            c.ConsultationFee,
            c.FollowUpFee,
            c.ProcedureFee)).ToList();

        return Result.Success<IReadOnlyList<ClinicDto>>(dtos);
    }
}
