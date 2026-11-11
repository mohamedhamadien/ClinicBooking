using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Features.Notifications.Commands.SendAppointmentReminder;
using ClinicBooking.Application.Features.Notifications.DTOs;
using ClinicBooking.Application.Features.Notifications.Queries.GetNotificationLogs;
using ClinicBooking.Application.Features.Notifications.Queries.GetWhatsAppLink;
using ClinicBooking.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicBooking.API.Controllers;

[Authorize]
public sealed class NotificationsController : ApiControllerBase
{
    /// <summary>
    /// Send an appointment reminder via WhatsApp, SMS, or Email.
    /// </summary>
    [HttpPost("remind")]
    [Authorize(Roles = $"{nameof(UserRole.Admin)},{nameof(UserRole.ClinicAdmin)},{nameof(UserRole.Doctor)},{nameof(UserRole.Receptionist)}")]
    [ProducesResponseType(typeof(NotificationLogDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendReminder([FromBody] SendAppointmentReminderCommand command, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Generate a 1-click WhatsApp web link with a personalized pre-composed reminder message.
    /// </summary>
    [HttpGet("whatsapp-link/{appointmentId:guid}")]
    [Authorize(Roles = $"{nameof(UserRole.Admin)},{nameof(UserRole.ClinicAdmin)},{nameof(UserRole.Doctor)},{nameof(UserRole.Receptionist)}")]
    [ProducesResponseType(typeof(WhatsAppReminderLinkDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetWhatsAppLink([FromRoute] Guid appointmentId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetWhatsAppLinkQuery(appointmentId), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Get paginated logs of sent/attempted appointment notifications.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = $"{nameof(UserRole.Admin)},{nameof(UserRole.ClinicAdmin)},{nameof(UserRole.Receptionist)}")]
    [ProducesResponseType(typeof(PagedResult<NotificationLogDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLogs(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] NotificationChannel? channel = null,
        [FromQuery] NotificationStatus? status = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetNotificationLogsQuery(pageNumber, pageSize, channel, status, search), cancellationToken);
        return HandleResult(result);
    }
}
