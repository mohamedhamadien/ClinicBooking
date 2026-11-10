using ClinicBooking.Application.Contracts.Services;
using ClinicBooking.Application.Features.Staff.Commands.CreateStaffMember;
using ClinicBooking.Application.Features.Staff.Commands.ToggleStaffStatus;
using ClinicBooking.Application.Features.Staff.Commands.UpdateStaffPermissions;
using ClinicBooking.Application.Features.Staff.DTOs;
using ClinicBooking.Application.Features.Staff.Queries.GetPermissionCatalog;
using ClinicBooking.Application.Features.Staff.Queries.GetStaffMembers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicBooking.API.Controllers;

[Authorize(Roles = "Admin,ClinicAdmin")]
public sealed class StaffController : ApiControllerBase
{
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserService _currentUserService;

    public StaffController(ITenantContext tenantContext, ICurrentUserService currentUserService)
    {
        _tenantContext = tenantContext;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// List all staff members in the current tenant or clinic.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<StaffMemberDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<StaffMemberDto>>> GetStaff(
        [FromQuery] Guid? clinicId,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;

        // Scope to clinic if caller is ClinicAdmin
        var effectiveClinicId = _currentUserService.Role == "ClinicAdmin" && _currentUserService.ClinicId.HasValue
            ? _currentUserService.ClinicId
            : clinicId;

        var query = new GetStaffMembersQuery(tenantId, effectiveClinicId);
        var result = await Mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Retrieve the standard permissions catalog and role presets.
    /// </summary>
    [HttpGet("permissions-catalog")]
    [ProducesResponseType(typeof(PermissionCatalogDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PermissionCatalogDto>> GetPermissionsCatalog(CancellationToken cancellationToken)
    {
        var query = new GetPermissionCatalogQuery();
        var result = await Mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Create a new staff account with designated role and permissions.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(StaffMemberDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StaffMemberDto>> CreateStaff(
        [FromBody] CreateStaffRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId;

        var effectiveClinicId = _currentUserService.Role == "ClinicAdmin" && _currentUserService.ClinicId.HasValue
            ? _currentUserService.ClinicId
            : request.ClinicId;

        var command = new CreateStaffMemberCommand(
            tenantId,
            request.Email,
            request.Password,
            request.FirstName,
            request.LastName,
            request.Role,
            effectiveClinicId,
            request.Permissions,
            _currentUserService.Role);

        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Update granted permissions and role for a staff member.
    /// </summary>
    [HttpPut("{id:guid}/permissions")]
    [ProducesResponseType(typeof(StaffMemberDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StaffMemberDto>> UpdatePermissions(
        Guid id,
        [FromBody] UpdateStaffPermissionsRequest request,
        CancellationToken cancellationToken)
    {
        var effectiveClinicId = _currentUserService.Role == "ClinicAdmin" && _currentUserService.ClinicId.HasValue
            ? _currentUserService.ClinicId
            : request.ClinicId;

        var command = new UpdateStaffPermissionsCommand(
            id,
            request.Role,
            effectiveClinicId,
            request.Permissions ?? [],
            _currentUserService.Role,
            _currentUserService.ClinicId);

        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Activate or deactivate a staff member account.
    /// </summary>
    [HttpPut("{id:guid}/status")]
    [ProducesResponseType(typeof(StaffMemberDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StaffMemberDto>> ToggleStatus(
        Guid id,
        CancellationToken cancellationToken)
    {
        var command = new ToggleStaffStatusCommand(
            id,
            _currentUserService.Role,
            _currentUserService.ClinicId);
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }
}

public sealed record CreateStaffRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string Role,
    Guid? ClinicId,
    IReadOnlyList<string>? Permissions);

public sealed record UpdateStaffPermissionsRequest(
    string? Role,
    Guid? ClinicId,
    IReadOnlyList<string>? Permissions);
