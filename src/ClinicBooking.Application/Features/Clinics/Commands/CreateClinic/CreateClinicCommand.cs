using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Contracts.Services;
using ClinicBooking.Application.Features.Clinics.DTOs;
using ClinicBooking.Domain.Entities;
using FluentValidation;
using MediatR;

namespace ClinicBooking.Application.Features.Clinics.Commands.CreateClinic;

public sealed record CreateClinicCommand(
    Guid TenantId,
    string Name,
    string Address,
    string City,
    string PhoneNumber,
    string? NameAr = null,
    string? AddressAr = null,
    string? CityAr = null,
    DateTime? SubscriptionStartDateUtc = null,
    DateTime? SubscriptionEndDateUtc = null,
    string? SubscriptionPlan = "Enterprise",
    bool IsPrivateClinic = true,
    bool IsSingleDoctorClinic = true,
    bool EnableProceduresDuringShifts = true,
    bool EnableGeneralClinicFeatures = false,
    decimal ConsultationFee = 350m,
    decimal FollowUpFee = 100m,
    decimal ProcedureFee = 1200m) : IRequest<Result<ClinicDto>>;

public sealed class CreateClinicCommandValidator : AbstractValidator<CreateClinicCommand>
{
    public CreateClinicCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(200);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PhoneNumber).NotEmpty();
        RuleFor(x => x.NameAr).MaximumLength(150);
        RuleFor(x => x.AddressAr).MaximumLength(200);
        RuleFor(x => x.CityAr).MaximumLength(100);
    }
}

public sealed class CreateClinicCommandHandler : IRequestHandler<CreateClinicCommand, Result<ClinicDto>>
{
    private readonly IClinicRepository _clinicRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ILocalizationService _localizationService;
    private readonly IUnitOfWork _unitOfWork;

    public CreateClinicCommandHandler(
        IClinicRepository clinicRepository,
        ITenantRepository tenantRepository,
        ITenantContext tenantContext,
        ILocalizationService localizationService,
        IUnitOfWork unitOfWork)
    {
        _clinicRepository = clinicRepository;
        _tenantRepository = tenantRepository;
        _tenantContext = tenantContext;
        _localizationService = localizationService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ClinicDto>> Handle(CreateClinicCommand request, CancellationToken cancellationToken)
    {
        var effectiveTenantId = request.TenantId != Guid.Empty
            ? request.TenantId
            : (_tenantContext.TenantId ?? Guid.Empty);

        var tenant = effectiveTenantId != Guid.Empty
            ? await _tenantRepository.GetByIdAsync(effectiveTenantId, cancellationToken)
            : null;

        if (tenant is null)
        {
            var allTenants = await _tenantRepository.ListAsync(cancellationToken);
            tenant = allTenants.FirstOrDefault(t => t.IsActive) ?? allTenants.FirstOrDefault();
        }

        if (tenant is null || !tenant.IsActive)
        {
            return Result.Failure<ClinicDto>(Error.NotFound("Tenant", effectiveTenantId));
        }

        var clinic = Clinic.Create(
            tenant.Id,
            request.Name,
            request.Address,
            request.City,
            request.PhoneNumber,
            request.NameAr,
            request.AddressAr,
            request.CityAr);

        clinic.SetSubscription(
            request.SubscriptionStartDateUtc ?? DateTime.UtcNow.Date,
            request.SubscriptionEndDateUtc ?? DateTime.UtcNow.Date.AddYears(1),
            request.SubscriptionPlan ?? "Enterprise");

        clinic.SetOperatingModeAndPricing(
            request.IsPrivateClinic,
            request.IsSingleDoctorClinic,
            request.EnableProceduresDuringShifts,
            request.EnableGeneralClinicFeatures,
            request.ConsultationFee,
            request.FollowUpFee,
            request.ProcedureFee);

        await _clinicRepository.AddAsync(clinic, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var isArabic = _localizationService.IsArabic;

        var dto = new ClinicDto(
            clinic.Id,
            clinic.TenantId,
            clinic.NameEn,
            clinic.AddressEn,
            clinic.CityEn,
            clinic.PhoneNumber.Value,
            clinic.IsActive,
            clinic.NameAr,
            clinic.AddressAr,
            clinic.CityAr,
            clinic.GetLocalizedName(isArabic),
            clinic.GetLocalizedAddress(isArabic),
            clinic.GetLocalizedCity(isArabic),
            clinic.SubscriptionStartDateUtc,
            clinic.SubscriptionEndDateUtc,
            clinic.SubscriptionPlan ?? "Enterprise",
            clinic.IsSubscriptionActive,
            clinic.SubscriptionDaysRemaining,
            clinic.IsPrivateClinic,
            clinic.IsSingleDoctorClinic,
            clinic.EnableProceduresDuringShifts,
            clinic.EnableGeneralClinicFeatures,
            clinic.ConsultationFee,
            clinic.FollowUpFee,
            clinic.ProcedureFee);

        return Result.Success(dto);
    }
}
