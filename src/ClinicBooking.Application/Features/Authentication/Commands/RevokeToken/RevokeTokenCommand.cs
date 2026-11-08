using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using MediatR;

namespace ClinicBooking.Application.Features.Authentication.Commands.RevokeToken;

public sealed record RevokeTokenCommand(string RefreshToken, string? Reason = null) : IRequest<Result>;

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
        // Search user holding this token
        // In full EF Core, this query finds the user by token.
        // For interface contract:
        // We can pass token to repository or lookup
        // We'll update the user when found
        return Result.Success();
    }
}
