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
    string? CityAr = null) : IRequest<Result<ClinicDto>>;

public sealed class CreateClinicCommandValidator : AbstractValidator<CreateClinicCommand>
{
    public CreateClinicCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty().WithMessage("TenantId is required.");
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
    private readonly ILocalizationService _localizationService;
    private readonly IUnitOfWork _unitOfWork;

    public CreateClinicCommandHandler(
        IClinicRepository clinicRepository,
        ITenantRepository tenantRepository,
        ILocalizationService localizationService,
        IUnitOfWork unitOfWork)
    {
        _clinicRepository = clinicRepository;
        _tenantRepository = tenantRepository;
        _localizationService = localizationService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ClinicDto>> Handle(CreateClinicCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);
        if (tenant is null || !tenant.IsActive)
        {
            return Result.Failure<ClinicDto>(Error.NotFound("Tenant", request.TenantId));
        }

        var clinic = Clinic.Create(
            request.TenantId,
            request.Name,
            request.Address,
            request.City,
            request.PhoneNumber,
            request.NameAr,
            request.AddressAr,
            request.CityAr);

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
            clinic.GetLocalizedCity(isArabic));

        return Result.Success(dto);
    }
}
