using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Contracts.Services;
using ClinicBooking.Application.Features.Authentication.DTOs;
using FluentValidation;
using MediatR;

namespace ClinicBooking.Application.Features.Authentication.Commands.Login;

public sealed record LoginCommand(
    string Email,
    string Password,
    string? TenantIdentifier = null) : IRequest<Result<AuthResponseDto>>;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResponseDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public LoginCommandHandler(
        IUserRepository userRepository,
        ITenantRepository tenantRepository,
        ITokenService tokenService,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _tenantRepository = tenantRepository;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AuthResponseDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

        // Same generic error for unknown user, inactive user and wrong password to prevent account enumeration.
        if (user is null
            || !user.IsActive
            || string.IsNullOrEmpty(user.PasswordHash)
            || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            return Result.Failure<AuthResponseDto>(Error.Unauthorized("Invalid email or password."));
        }

        string? tenantSlug = null;
        if (user.TenantId.HasValue)
        {
            var tenant = await _tenantRepository.GetByIdAsync(user.TenantId.Value, cancellationToken);
            if (tenant is null || !tenant.IsActive)
            {
                return Result.Failure<AuthResponseDto>(Error.Unauthorized("Tenant is deactivated or not found."));
            }

            // If the caller targeted a specific tenant, the user must belong to it.
            if (!string.IsNullOrWhiteSpace(request.TenantIdentifier)
                && !tenant.Identifier.Equals(request.TenantIdentifier.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return Result.Failure<AuthResponseDto>(Error.Unauthorized("Invalid email or password."));
            }

            tenantSlug = tenant.Identifier;
        }

        var (refreshToken, jwtId, refreshExpiresUtc) = _tokenService.GenerateRefreshToken();
        user.AddRefreshToken(refreshToken, jwtId, refreshExpiresUtc, createdByIp: null);

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var accessToken = _tokenService.GenerateAccessToken(user, tenantSlug);

        var userDto = new UserDto(
            user.Id,
            user.TenantId,
            user.Email.Value,
            user.FirstName,
            user.LastName,
            user.Role.ToString(),
            user.IsActive,
            user.ClinicId,
            user.GetEffectivePermissions());

        return Result.Success(new AuthResponseDto(
            accessToken,
            refreshToken,
            refreshExpiresUtc,
            userDto));
    }
}
