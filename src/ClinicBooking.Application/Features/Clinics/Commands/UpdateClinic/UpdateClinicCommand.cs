using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Contracts.Services;
using ClinicBooking.Application.Features.Clinics.DTOs;
using ClinicBooking.Domain.ValueObjects;
using FluentValidation;
using MediatR;

namespace ClinicBooking.Application.Features.Clinics.Commands.UpdateClinic;

public sealed record UpdateClinicCommand(
    Guid Id,
    string Name,
    string Address,
    string City,
    string PhoneNumber,
    bool IsActive = true,
    string? NameAr = null,
    string? AddressAr = null,
    string? CityAr = null) : IRequest<Result<ClinicDto>>;

public sealed class UpdateClinicCommandValidator : AbstractValidator<UpdateClinicCommand>
{
    public UpdateClinicCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Clinic ID is required.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(200);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PhoneNumber).NotEmpty();
        RuleFor(x => x.NameAr).MaximumLength(150);
        RuleFor(x => x.AddressAr).MaximumLength(200);
        RuleFor(x => x.CityAr).MaximumLength(100);
    }
}

public sealed class UpdateClinicCommandHandler : IRequestHandler<UpdateClinicCommand, Result<ClinicDto>>
{
    private readonly IClinicRepository _clinicRepository;
    private readonly ILocalizationService _localizationService;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateClinicCommandHandler(
        IClinicRepository clinicRepository,
        ILocalizationService localizationService,
        IUnitOfWork unitOfWork)
    {
        _clinicRepository = clinicRepository;
        _localizationService = localizationService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ClinicDto>> Handle(UpdateClinicCommand request, CancellationToken cancellationToken)
    {
        var clinic = await _clinicRepository.GetByIdAsync(request.Id, cancellationToken);
        if (clinic is null)
        {
            return Result.Failure<ClinicDto>(Error.NotFound("Clinic", request.Id));
        }

        clinic.Update(
            request.Name,
            request.Address,
            request.City,
            PhoneNumber.Create(request.PhoneNumber),
            request.IsActive,
            request.NameAr,
            request.AddressAr,
            request.CityAr);

        await _clinicRepository.UpdateAsync(clinic, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
