using ClinicBooking.Application.Contracts.Services;
using ClinicBooking.Application.Features.Admin.DTOs;
using ClinicBooking.Application.Features.Admin.Queries.GetAllAppointments;
using ClinicBooking.Application.Features.Admin.Queries.GetDashboardStats;
using ClinicBooking.Application.Features.Appointments.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicBooking.API.Controllers;

[Authorize(Roles = "Admin")]
public sealed class AdminController : ApiControllerBase
{
    private readonly ITenantContext _tenantContext;

    public AdminController(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// View all clinic appointments across doctors and dates (Admin only).
    /// </summary>
    [HttpGet("appointments")]
    [ProducesResponseType(typeof(IReadOnlyList<AppointmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AppointmentDto>>> GetAllAppointments(
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] Guid? doctorId,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId ?? Guid.Empty;
        var query = new GetAllAppointmentsQuery(tenantId, fromUtc, toUtc, doctorId);
        var result = await Mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Retrieve operational dashboard metrics and counts (Admin only).
    /// </summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(DashboardStatsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardStatsDto>> GetDashboardStats(CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId ?? Guid.Empty;
        var query = new GetDashboardStatsQuery(tenantId);
        var result = await Mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }
}
