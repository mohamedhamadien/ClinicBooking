using ClinicBooking.Application.Contracts.Services;
using ClinicBooking.Application.Features.Admin.DTOs;
using ClinicBooking.Application.Features.Admin.Queries.GetAllAppointments;
using ClinicBooking.Application.Features.Admin.Queries.GetAuditLogs;
using ClinicBooking.Application.Features.Admin.Queries.GetDashboardStats;
using ClinicBooking.Application.Features.Appointments.DTOs;
using ClinicBooking.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicBooking.API.Controllers;

[Authorize(Roles = "Admin,ClinicAdmin,Staff,Receptionist,Nurse,Accountant,Doctor")]
public sealed class AdminController : ApiControllerBase
{
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserService _currentUserService;

    public AdminController(ITenantContext tenantContext, ICurrentUserService currentUserService)
    {
        _tenantContext = tenantContext;
        _currentUserService = currentUserService;
    }

    private Guid? ResolveScopedClinicId(Guid? requestedClinicId)
    {
        if (_currentUserService.Role is not "Admin" && _currentUserService.ClinicId.HasValue)
        {
            return _currentUserService.ClinicId;
        }

        return requestedClinicId;
    }

    /// <summary>
    /// View clinic appointments across doctors and dates, scoped to assigned clinic for non-global staff.
    /// </summary>
    [HttpGet("appointments")]
    [ProducesResponseType(typeof(IReadOnlyList<AppointmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AppointmentDto>>> GetAllAppointments(
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] Guid? doctorId,
        [FromQuery] Guid? clinicId,
        [FromQuery] AppointmentStatus? status,
        [FromQuery] AppointmentSource? source,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId ?? Guid.Empty;
        var effectiveClinicId = ResolveScopedClinicId(clinicId);

        var query = new GetAllAppointmentsQuery(tenantId, fromUtc, toUtc, doctorId, effectiveClinicId, status, source);
        var result = await Mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Retrieve operational dashboard metrics and counts.
    /// </summary>
    [HttpGet("dashboard")]
    [HttpGet("dashboard-stats")]
    [ProducesResponseType(typeof(DashboardStatsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardStatsDto>> GetDashboardStats(
        [FromQuery] Guid? clinicId,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId ?? Guid.Empty;
        var effectiveClinicId = ResolveScopedClinicId(clinicId);

        var query = new GetDashboardStatsQuery(tenantId, effectiveClinicId);
        var result = await Mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// View audit trail logs for clinical and administrative data changes (Admin and ClinicAdmin only).
    /// </summary>
    [HttpGet("audit-logs")]
    [Authorize(Roles = "Admin,ClinicAdmin")]
    [ProducesResponseType(typeof(IReadOnlyList<AuditLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AuditLogDto>>> GetAuditLogs(
        [FromQuery] string? entityName,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        var query = new GetAuditLogsQuery(tenantId, entityName, limit);
        var result = await Mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }
}
