using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Contracts.Services;
using ClinicBooking.Application.Features.Doctors.DTOs;
using ClinicBooking.Domain.Entities;
using FluentValidation;
using MediatR;

namespace ClinicBooking.Application.Features.Doctors.Commands.CreateDoctor;

public sealed record CreateDoctorCommand(
    Guid TenantId,
    Guid UserId,
    Guid SpecialtyId,
    string LicenseNumber,
    string Bio,
    int DefaultSlotDurationMinutes = 30,
    bool RequiresApproval = false,
    string? BioAr = null) : IRequest<Result<DoctorDto>>;

public sealed class CreateDoctorCommandValidator : AbstractValidator<CreateDoctorCommand>
{
    public CreateDoctorCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty().WithMessage("TenantId is required.");
        RuleFor(x => x.UserId).NotEmpty().WithMessage("UserId is required.");
        RuleFor(x => x.SpecialtyId).NotEmpty().WithMessage("SpecialtyId is required.");
        RuleFor(x => x.LicenseNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.DefaultSlotDurationMinutes)
            .InclusiveBetween(10, 180)
            .WithMessage("Default slot duration must be between 10 and 180 minutes.");
        RuleFor(x => x.Bio).MaximumLength(2000);
        RuleFor(x => x.BioAr).MaximumLength(2000);
    }
}

public sealed class CreateDoctorCommandHandler : IRequestHandler<CreateDoctorCommand, Result<DoctorDto>>
{
    private readonly IDoctorRepository _doctorRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly ISpecialtyRepository _specialtyRepository;
    private readonly ILocalizationService _localizationService;
    private readonly IUnitOfWork _unitOfWork;

    public CreateDoctorCommandHandler(
        IDoctorRepository doctorRepository,
        ITenantRepository tenantRepository,
        ISpecialtyRepository specialtyRepository,
        ILocalizationService localizationService,
        IUnitOfWork unitOfWork)
    {
        _doctorRepository = doctorRepository;
        _tenantRepository = tenantRepository;
        _specialtyRepository = specialtyRepository;
        _localizationService = localizationService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DoctorDto>> Handle(CreateDoctorCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);
        if (tenant is null || !tenant.IsActive)
        {
            return Result.Failure<DoctorDto>(Error.NotFound("Tenant", request.TenantId));
        }

        var specialty = await _specialtyRepository.GetByIdAsync(request.SpecialtyId, cancellationToken);
        if (specialty is null || !specialty.IsActive)
        {
            return Result.Failure<DoctorDto>(Error.NotFound("Specialty", request.SpecialtyId));
        }

        var doctor = Doctor.Create(
            request.TenantId,
            request.UserId,
            request.SpecialtyId,
            request.LicenseNumber,
            request.Bio,
            request.DefaultSlotDurationMinutes,
            request.RequiresApproval,
            request.BioAr);

        await _doctorRepository.AddAsync(doctor, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var isArabic = _localizationService.IsArabic;

        var dto = new DoctorDto(
            doctor.Id,
            doctor.TenantId,
            doctor.UserId,
            doctor.SpecialtyId,
            doctor.LicenseNumber,
            doctor.BioEn,
            doctor.DefaultSlotDurationMinutes,
            doctor.RequiresApproval,
            doctor.IsActive,
            [],
            doctor.BioAr,
            doctor.GetLocalizedBio(isArabic));

        return Result.Success(dto);
    }
}
