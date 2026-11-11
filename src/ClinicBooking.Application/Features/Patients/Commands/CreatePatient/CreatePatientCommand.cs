using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Contracts.Services;
using ClinicBooking.Application.Features.Patients.DTOs;
using ClinicBooking.Domain.Entities;
using ClinicBooking.Domain.Enums;
using FluentValidation;
using MediatR;

namespace ClinicBooking.Application.Features.Patients.Commands.CreatePatient;

public sealed record CreatePatientCommand(
    Guid TenantId,
    string FirstName,
    string LastName,
    string? Email,
    string? PhoneNumber,
    DateOnly DateOfBirth,
    Gender Gender,
    string? BloodGroup = null,
    string? Allergies = null,
    string? ChronicConditions = null,
    string? EmergencyContact = null,
    string? Password = null) : IRequest<Result<PatientSummaryDto>>;

public sealed class CreatePatientCommandValidator : AbstractValidator<CreatePatientCommand>
{
    public CreatePatientCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().WithMessage("First name is required.").MaximumLength(50);
        RuleFor(x => x.LastName).NotEmpty().WithMessage("Last name is required.").MaximumLength(50);
        RuleFor(x => x.DateOfBirth).NotEmpty().WithMessage("Date of birth is required.");
        When(x => !string.IsNullOrWhiteSpace(x.Email), () =>
        {
            RuleFor(x => x.Email).EmailAddress().WithMessage("A valid email address is required.");
        });
    }
}

public sealed class CreatePatientCommandHandler : IRequestHandler<CreatePatientCommand, Result<PatientSummaryDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public CreatePatientCommandHandler(
        IUserRepository userRepository,
        IPatientRepository patientRepository,
        ITenantRepository tenantRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _patientRepository = patientRepository;
        _tenantRepository = tenantRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PatientSummaryDto>> Handle(
        CreatePatientCommand request,
        CancellationToken cancellationToken)
    {
        // Resolve tenant
        var tenantId = request.TenantId;
        if (tenantId == Guid.Empty)
        {
            var defaultTenant = await _tenantRepository.GetByIdentifierAsync("main", cancellationToken);
            tenantId = defaultTenant?.Id ?? Guid.Empty;
        }

        var effectiveEmail = !string.IsNullOrWhiteSpace(request.Email)
            ? request.Email.Trim().ToLowerInvariant()
            : $"patient.{Guid.NewGuid().ToString("N")[..8]}@clinic.local";

        if (await _userRepository.ExistsByEmailAsync(effectiveEmail, cancellationToken))
        {
            return Result.Failure<PatientSummaryDto>(
                Error.Conflict("Patient.DuplicateEmail", $"A patient with email '{effectiveEmail}' is already registered."));
        }

        var user = User.Create(
            effectiveEmail,
            request.FirstName.Trim(),
            request.LastName.Trim(),
            UserRole.Patient,
            tenantId);

        var plainPassword = !string.IsNullOrWhiteSpace(request.Password)
            ? request.Password
            : "Patient@123";

        user.SetPasswordHash(_passwordHasher.HashPassword(plainPassword));

        var contactNumber = !string.IsNullOrWhiteSpace(request.PhoneNumber)
            ? request.PhoneNumber.Trim()
            : request.EmergencyContact?.Trim();

        var patient = Patient.Create(
            tenantId,
            user.Id,
            request.DateOfBirth,
            request.Gender,
            contactNumber);

        patient.UpdateMedicalHistory(
            request.BloodGroup,
            request.Allergies,
            request.ChronicConditions,
            "Patient record created at reception/clinic administration.");

        await _userRepository.AddAsync(user, cancellationToken);
        await _patientRepository.AddAsync(patient, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new PatientSummaryDto(
            patient.Id,
            user.Id,
            user.FullName,
            user.Email.Value,
            contactNumber,
            patient.DateOfBirth,
            patient.Gender.ToString(),
            patient.BloodGroup,
            patient.Allergies,
            patient.ChronicConditions,
            contactNumber,
            patient.CreatedAtUtc);

        return Result.Success(dto);
    }
}
