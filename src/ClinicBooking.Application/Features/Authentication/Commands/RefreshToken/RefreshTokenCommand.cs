using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Contracts.Services;
using ClinicBooking.Application.Features.Authentication.DTOs;
using FluentValidation;
using MediatR;

namespace ClinicBooking.Application.Features.Authentication.Commands.RefreshToken;

public sealed record RefreshTokenCommand(
    string AccessToken,
    string RefreshToken) : IRequest<Result<AuthResponseDto>>;

public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.AccessToken).NotEmpty();
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResponseDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _unitOfWork;

    public RefreshTokenCommandHandler(
        IUserRepository userRepository,
        ITenantRepository tenantRepository,
        ITokenService tokenService,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _tenantRepository = tenantRepository;
        _tokenService = tokenService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AuthResponseDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var userId = _tokenService.ValidateExpiredToken(request.AccessToken);
        if (userId is null)
        {
            return Result.Failure<AuthResponseDto>(Error.Unauthorized("Invalid access token."));
        }

        var user = await _userRepository.GetByIdAsync(userId.Value, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return Result.Failure<AuthResponseDto>(Error.Unauthorized("User not found or inactive."));
        }

        var existingRefreshToken = user.RefreshTokens.FirstOrDefault(t => t.Token == request.RefreshToken);
        if (existingRefreshToken is null)
        {
            return Result.Failure<AuthResponseDto>(Error.Unauthorized("Invalid refresh token."));
        }

        if (existingRefreshToken.IsRevoked)
        {
            // Security: Refresh token replay attack detected! Revoke all tokens for this user.
            foreach (var token in user.RefreshTokens.Where(t => t.IsActive))
            {
                token.Revoke(DateTime.UtcNow, null, "Revoked due to compromised token reuse detection.");
            }

            await _userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Failure<AuthResponseDto>(Error.Unauthorized("Compromised token detected. All sessions revoked."));
        }

        if (existingRefreshToken.IsExpired)
        {
            return Result.Failure<AuthResponseDto>(Error.Unauthorized("Refresh token has expired. Please login again."));
        }

        // Token rotation: Revoke old token and create new one
        var (newRefreshToken, newJwtId, newExpiresUtc) = _tokenService.GenerateRefreshToken();
        user.RevokeRefreshToken(request.RefreshToken, revokedByIp: null, reason: "Rotated", replacedByToken: newRefreshToken);
        user.AddRefreshToken(newRefreshToken, newJwtId, newExpiresUtc, createdByIp: null);

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        string? tenantSlug = null;
        if (user.TenantId.HasValue)
        {
            var tenant = await _tenantRepository.GetByIdAsync(user.TenantId.Value, cancellationToken);
            tenantSlug = tenant?.Identifier;
        }

        var newAccessToken = _tokenService.GenerateAccessToken(user, tenantSlug);

        var userDto = new UserDto(
            user.Id,
            user.TenantId,
            user.Email.Value,
            user.FirstName,
            user.LastName,
            user.Role.ToString(),
            user.IsActive,
            user.ClinicId);

        return Result.Success(new AuthResponseDto(
            newAccessToken,
            newRefreshToken,
            newExpiresUtc,
            userDto));
    }
}
