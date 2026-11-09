using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Contracts.Services;
using ClinicBooking.Application.Features.Doctors.DTOs;
using ClinicBooking.Domain.Entities;
using ClinicBooking.Domain.Enums;
using FluentValidation;
using MediatR;

namespace ClinicBooking.Application.Features.Doctors.Commands.RegisterDoctor;

public sealed record RegisterDoctorCommand(
    Guid TenantId,
    string FirstName,
    string LastName,
    string Email,
    string Password,
    Guid SpecialtyId,
    string LicenseNumber,
    string BioEn,
    string? BioAr = null,
    Guid? ClinicId = null,
    decimal ConsultationFee = 0,
    int DefaultSlotDurationMinutes = 30,
    bool RequiresApproval = false,
    DayOfWeek? InitialShiftDay = null,
    TimeOnly? InitialShiftStartTime = null,
    TimeOnly? InitialShiftEndTime = null) : IRequest<Result<DoctorDto>>;

public sealed class RegisterDoctorCommandValidator : AbstractValidator<RegisterDoctorCommand>
{
    public RegisterDoctorCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty().WithMessage("TenantId is required.");
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("A valid email address is required.");
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.");
        RuleFor(x => x.SpecialtyId).NotEmpty().WithMessage("SpecialtyId is required.");
        RuleFor(x => x.LicenseNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.BioEn).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.BioAr).MaximumLength(2000);
        RuleFor(x => x.ConsultationFee).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DefaultSlotDurationMinutes)
            .InclusiveBetween(10, 180)
            .WithMessage("Default slot duration must be between 10 and 180 minutes.");
    }
}

public sealed class RegisterDoctorCommandHandler : IRequestHandler<RegisterDoctorCommand, Result<DoctorDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IDoctorRepository _doctorRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly ISpecialtyRepository _specialtyRepository;
    private readonly IClinicRepository _clinicRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILocalizationService _localizationService;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterDoctorCommandHandler(
        IUserRepository userRepository,
        IDoctorRepository doctorRepository,
        ITenantRepository tenantRepository,
        ISpecialtyRepository specialtyRepository,
        IClinicRepository clinicRepository,
        IPasswordHasher passwordHasher,
        ILocalizationService localizationService,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _doctorRepository = doctorRepository;
        _tenantRepository = tenantRepository;
        _specialtyRepository = specialtyRepository;
        _clinicRepository = clinicRepository;
        _passwordHasher = passwordHasher;
        _localizationService = localizationService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DoctorDto>> Handle(RegisterDoctorCommand request, CancellationToken cancellationToken)
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

        if (await _userRepository.ExistsByEmailAsync(request.Email, cancellationToken))
        {
            return Result.Failure<DoctorDto>(Error.Conflict("User.DuplicateEmail", $"User with email '{request.Email}' already exists."));
        }

        Clinic? clinic = null;
        if (request.ClinicId.HasValue)
        {
            clinic = await _clinicRepository.GetByIdAsync(request.ClinicId.Value, cancellationToken);
            if (clinic is null || clinic.TenantId != request.TenantId)
            {
                return Result.Failure<DoctorDto>(Error.NotFound("Clinic", request.ClinicId.Value));
            }
        }

        // 1. Create User Account for the Doctor
        var user = User.Create(
            email: request.Email,
            firstName: request.FirstName,
            lastName: request.LastName,
            role: UserRole.Doctor,
            tenantId: request.TenantId,
            clinicId: request.ClinicId);

        user.SetPasswordHash(_passwordHasher.HashPassword(request.Password));
        await _userRepository.AddAsync(user, cancellationToken);

        // 2. Create Doctor Profile
        var doctor = Doctor.Create(
            tenantId: request.TenantId,
            userId: user.Id,
            specialtyId: request.SpecialtyId,
            licenseNumber: request.LicenseNumber,
            bioEn: request.BioEn,
            defaultSlotDurationMinutes: request.DefaultSlotDurationMinutes,
            requiresApproval: request.RequiresApproval,
            bioAr: request.BioAr);

        // 3. Assign to Clinic if provided
        if (request.ClinicId.HasValue)
        {
            doctor.AssignToClinic(request.ClinicId.Value, request.ConsultationFee, isPrimary: true);

            // 4. Add initial working shift if provided
            if (request.InitialShiftDay.HasValue &&
                request.InitialShiftStartTime.HasValue &&
                request.InitialShiftEndTime.HasValue)
            {
                doctor.AddAvailability(
                    request.ClinicId.Value,
                    request.InitialShiftDay.Value,
                    request.InitialShiftStartTime.Value,
                    request.InitialShiftEndTime.Value);
            }
        }

        await _doctorRepository.AddAsync(doctor, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var isArabic = _localizationService.IsArabic;

        var docClinics = doctor.DoctorClinics.Select(dc => new DoctorClinicDto(
            dc.ClinicId,
            clinic?.GetLocalizedName(isArabic) ?? "Clinic Branch",
            clinic?.GetLocalizedCity(isArabic) ?? "City",
            dc.ConsultationFee,
            dc.IsPrimary)).ToList();

        var availabilities = doctor.Availabilities.Select(a => new DoctorAvailabilityDto(
            a.Id,
            a.ClinicId,
            a.DayOfWeek,
            a.Shift.StartTime,
            a.Shift.EndTime,
            a.IsActive,
            _localizationService.GetDayOfWeekName(a.DayOfWeek),
            clinic?.GetLocalizedName(isArabic),
            new DoctorShiftDto(a.Shift.StartTime, a.Shift.EndTime))).ToList();

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
            availabilities,
            doctor.BioAr,
            doctor.GetLocalizedBio(isArabic),
            user.FullName,
            user.Email.Value,
            specialty.GetLocalizedName(isArabic),
            docClinics);

        return Result.Success(dto);
    }
}
