namespace ClinicBooking.Application.Contracts.Services;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    string? Role { get; }
    Guid? ClinicId { get; }
    bool IsAuthenticated { get; }
    IReadOnlyList<string> Permissions { get; }
    bool HasPermission(string permission);
}
