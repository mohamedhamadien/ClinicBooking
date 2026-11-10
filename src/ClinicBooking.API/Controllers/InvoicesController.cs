using ClinicBooking.Application.Contracts.Services;
using ClinicBooking.Application.Features.Invoices.Commands.CreateInvoice;
using ClinicBooking.Application.Features.Invoices.Commands.PayInvoice;
using ClinicBooking.Application.Features.Invoices.DTOs;
using ClinicBooking.Application.Features.Invoices.Queries.GetInvoices;
using ClinicBooking.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicBooking.API.Controllers;

[Authorize(Roles = "Admin,ClinicAdmin,Staff,Accountant,Receptionist")]
public sealed class InvoicesController : ApiControllerBase
{
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserService _currentUserService;

    public InvoicesController(ITenantContext tenantContext, ICurrentUserService currentUserService)
    {
        _tenantContext = tenantContext;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Retrieve clinic invoices and financial records (Admin, ClinicAdmin).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<InvoiceDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<InvoiceDto>>> GetInvoices(
        [FromQuery] Guid? clinicId,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId ?? Guid.Empty;
        var effectiveClinicId = _currentUserService.Role == "ClinicAdmin" && _currentUserService.ClinicId.HasValue
            ? _currentUserService.ClinicId
            : clinicId;

        var query = new GetInvoicesQuery(tenantId, effectiveClinicId);
        var result = await Mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Issue a new invoice for consultation, procedures, or lab services (Admin, ClinicAdmin).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<InvoiceDto>> CreateInvoice(
        [FromBody] CreateInvoiceCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Record payment receipt for an invoice (Admin, ClinicAdmin).
    /// </summary>
    [HttpPost("{id:guid}/pay")]
    [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<InvoiceDto>> PayInvoice(
        [FromRoute] Guid id,
        [FromBody] PayInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        var command = new PayInvoiceCommand(id, request.PaymentMethod, request.Amount);
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }
}

public sealed record PayInvoiceRequest(PaymentMethod PaymentMethod, decimal? Amount = null);
