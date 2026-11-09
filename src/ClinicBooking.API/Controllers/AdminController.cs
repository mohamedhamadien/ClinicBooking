using ClinicBooking.Application.Contracts.Services;
using ClinicBooking.Application.Features.Admin.DTOs;
using ClinicBooking.Application.Features.Admin.Queries.GetAllAppointments;
using ClinicBooking.Application.Features.Admin.Queries.GetDashboardStats;
using ClinicBooking.Application.Features.Appointments.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicBooking.API.Controllers;

[Authorize(Roles = "Admin,ClinicAdmin")]
public sealed class AdminController : ApiControllerBase
{
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserService _currentUserService;

    public AdminController(ITenantContext tenantContext, ICurrentUserService currentUserService)
    {
        _tenantContext = tenantContext;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// View all clinic appointments across doctors and dates (Admin or ClinicAdmin).
    /// </summary>
    [HttpGet("appointments")]
    [ProducesResponseType(typeof(IReadOnlyList<AppointmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AppointmentDto>>> GetAllAppointments(
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] Guid? doctorId,
        [FromQuery] Guid? clinicId,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId ?? Guid.Empty;

        // If user is ClinicAdmin, scope appointments strictly to their clinic
        var effectiveClinicId = _currentUserService.Role == "ClinicAdmin" && _currentUserService.ClinicId.HasValue
            ? _currentUserService.ClinicId
            : clinicId;

        var query = new GetAllAppointmentsQuery(tenantId, fromUtc, toUtc, doctorId, effectiveClinicId);
        var result = await Mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Retrieve operational dashboard metrics and counts (Admin or ClinicAdmin).
    /// </summary>
    [HttpGet("dashboard")]
    [HttpGet("dashboard-stats")]
    [ProducesResponseType(typeof(DashboardStatsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardStatsDto>> GetDashboardStats(
        [FromQuery] Guid? clinicId,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId ?? Guid.Empty;

        var effectiveClinicId = _currentUserService.Role == "ClinicAdmin" && _currentUserService.ClinicId.HasValue
            ? _currentUserService.ClinicId
            : clinicId;

        var query = new GetDashboardStatsQuery(tenantId, effectiveClinicId);
        var result = await Mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }
}
