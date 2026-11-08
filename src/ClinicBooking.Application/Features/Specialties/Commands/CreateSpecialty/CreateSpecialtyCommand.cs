using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Contracts.Services;
using ClinicBooking.Application.Features.Specialties.DTOs;
using ClinicBooking.Domain.Entities;
using FluentValidation;
using MediatR;

namespace ClinicBooking.Application.Features.Specialties.Commands.CreateSpecialty;

public sealed record CreateSpecialtyCommand(
    string Name,
    string Description,
    Guid? TenantId = null,
    string? NameAr = null,
    string? DescriptionAr = null) : IRequest<Result<SpecialtyDto>>;

public sealed class CreateSpecialtyCommandValidator : AbstractValidator<CreateSpecialtyCommand>
{
    public CreateSpecialtyCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.NameAr).MaximumLength(100);
        RuleFor(x => x.DescriptionAr).MaximumLength(500);
    }
}

public sealed class CreateSpecialtyCommandHandler : IRequestHandler<CreateSpecialtyCommand, Result<SpecialtyDto>>
{
    private readonly ISpecialtyRepository _specialtyRepository;
    private readonly ILocalizationService _localizationService;
    private readonly IUnitOfWork _unitOfWork;

    public CreateSpecialtyCommandHandler(
        ISpecialtyRepository specialtyRepository,
        ILocalizationService localizationService,
        IUnitOfWork unitOfWork)
    {
        _specialtyRepository = specialtyRepository;
        _localizationService = localizationService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<SpecialtyDto>> Handle(CreateSpecialtyCommand request, CancellationToken cancellationToken)
    {
        var specialty = Specialty.Create(
            request.Name,
            request.Description,
            request.TenantId,
            request.NameAr,
            request.DescriptionAr);

        await _specialtyRepository.AddAsync(specialty, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var isArabic = _localizationService.IsArabic;

        var dto = new SpecialtyDto(
            specialty.Id,
            specialty.TenantId,
            specialty.NameEn,
            specialty.DescriptionEn,
            specialty.IsActive,
            specialty.NameAr,
            specialty.DescriptionAr,
            specialty.GetLocalizedName(isArabic),
            specialty.GetLocalizedDescription(isArabic));

        return Result.Success(dto);
    }
}
