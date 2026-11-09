using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Contracts.Services;
using ClinicBooking.Application.Features.Authentication.DTOs;
using ClinicBooking.Domain.Entities;
using ClinicBooking.Domain.Enums;
using FluentValidation;
using MediatR;

namespace ClinicBooking.Application.Features.Authentication.Commands.RegisterPatient;

public sealed record RegisterPatientCommand(
    Guid TenantId,
    string Email,
    string Password,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    Gender Gender,
    string? EmergencyContact = null) : IRequest<Result<AuthResponseDto>>;

public sealed class RegisterPatientCommandValidator : AbstractValidator<RegisterPatientCommand>
{
    public RegisterPatientCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty().WithMessage("TenantId is required.");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("A valid email address is required.");
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one number.");
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.DateOfBirth).NotEmpty();
    }
}

public sealed class RegisterPatientCommandHandler : IRequestHandler<RegisterPatientCommand, Result<AuthResponseDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterPatientCommandHandler(
        IUserRepository userRepository,
        IPatientRepository patientRepository,
        ITenantRepository tenantRepository,
        ITokenService tokenService,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _patientRepository = patientRepository;
        _tenantRepository = tenantRepository;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AuthResponseDto>> Handle(RegisterPatientCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);
        if (tenant is null || !tenant.IsActive)
        {
            return Result.Failure<AuthResponseDto>(Error.NotFound("Tenant", request.TenantId));
        }

        if (await _userRepository.ExistsByEmailAsync(request.Email, cancellationToken))
        {
            return Result.Failure<AuthResponseDto>(Error.Conflict("User.DuplicateEmail", "A user with this email already exists."));
        }

        var user = User.Create(
            request.Email,
            request.FirstName,
            request.LastName,
            UserRole.Patient,
            request.TenantId);

        user.SetPasswordHash(_passwordHasher.HashPassword(request.Password));

        var patient = Patient.Create(
            request.TenantId,
            user.Id,
            request.DateOfBirth,
            request.Gender,
            request.EmergencyContact);

        var (refreshToken, jwtId, refreshExpiresUtc) = _tokenService.GenerateRefreshToken();
        user.AddRefreshToken(refreshToken, jwtId, refreshExpiresUtc, createdByIp: null);

        await _userRepository.AddAsync(user, cancellationToken);
        await _patientRepository.AddAsync(patient, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var accessToken = _tokenService.GenerateAccessToken(user, tenant.Identifier);

        var userDto = new UserDto(
            user.Id,
            user.TenantId,
            user.Email.Value,
            user.FirstName,
            user.LastName,
            user.Role.ToString(),
            user.IsActive,
            user.ClinicId);

        var authResponse = new AuthResponseDto(
            accessToken,
            refreshToken,
            refreshExpiresUtc,
            userDto);

        return Result.Success(authResponse);
    }
}
