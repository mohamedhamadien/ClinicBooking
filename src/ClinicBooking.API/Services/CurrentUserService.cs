using System.Security.Claims;
using ClinicBooking.Application.Contracts.Services;

namespace ClinicBooking.API.Services;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var idClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User?.FindFirst("sub")?.Value;

            return Guid.TryParse(idClaim, out var id) ? id : null;
        }
    }

    public string? Email => User?.FindFirst(ClaimTypes.Email)?.Value ?? User?.FindFirst("email")?.Value;

    public string? Role => User?.FindFirst(ClaimTypes.Role)?.Value ?? User?.FindFirst("role")?.Value;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
}
