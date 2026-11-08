using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using FluentValidation;
using MediatR;

namespace ClinicBooking.Application.Features.Authentication.Commands.RevokeToken;

public sealed record RevokeTokenCommand(string RefreshToken, string? Reason = null) : IRequest<Result>;

public sealed class RevokeTokenCommandValidator : AbstractValidator<RevokeTokenCommand>
{
    public RevokeTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(200);
    }
}

public sealed class RevokeTokenCommandHandler : IRequestHandler<RevokeTokenCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RevokeTokenCommandHandler(IUserRepository userRepository, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByRefreshTokenAsync(request.RefreshToken, cancellationToken);
        var token = user?.FindRefreshToken(request.RefreshToken);

        if (user is null || token is null || !token.IsActive)
        {
            return Result.Failure(Error.Validation("Auth.InvalidToken", "Refresh token is invalid, expired or already revoked."));
        }

        user.RevokeRefreshToken(request.RefreshToken, revokedByIp: null, reason: request.Reason ?? "Revoked by user");

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
