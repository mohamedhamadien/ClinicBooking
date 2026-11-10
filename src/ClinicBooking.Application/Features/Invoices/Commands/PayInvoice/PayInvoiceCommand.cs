using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Features.Invoices.DTOs;
using ClinicBooking.Domain.Enums;
using FluentValidation;
using MediatR;

namespace ClinicBooking.Application.Features.Invoices.Commands.PayInvoice;

public sealed record PayInvoiceCommand(
    Guid InvoiceId,
    PaymentMethod PaymentMethod = PaymentMethod.Cash,
    decimal? Amount = null) : IRequest<Result<InvoiceDto>>;

public sealed class PayInvoiceCommandValidator : AbstractValidator<PayInvoiceCommand>
{
    public PayInvoiceCommandValidator()
    {
        RuleFor(x => x.InvoiceId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0).When(x => x.Amount.HasValue);
    }
}

public sealed class PayInvoiceCommandHandler : IRequestHandler<PayInvoiceCommand, Result<InvoiceDto>>
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IClinicRepository _clinicRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PayInvoiceCommandHandler(
        IInvoiceRepository invoiceRepository,
        IClinicRepository clinicRepository,
        IPatientRepository patientRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _invoiceRepository = invoiceRepository;
        _clinicRepository = clinicRepository;
        _patientRepository = patientRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<InvoiceDto>> Handle(PayInvoiceCommand request, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(request.InvoiceId, cancellationToken);
        if (invoice is null)
        {
            return Result.Failure<InvoiceDto>(Error.NotFound("Invoice", request.InvoiceId));
        }

        if (request.Amount.HasValue && request.Amount.Value > 0)
        {
            invoice.RecordPayment(request.Amount.Value, request.PaymentMethod, DateTime.UtcNow);
        }
        else
        {
            invoice.MarkAsPaid(request.PaymentMethod, DateTime.UtcNow);
        }
        await _invoiceRepository.UpdateAsync(invoice, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var clinic = await _clinicRepository.GetByIdAsync(invoice.ClinicId, cancellationToken);
        var patient = await _patientRepository.GetByIdAsync(invoice.PatientId, cancellationToken);
        string? patientName = null;
        if (patient is not null)
        {
            var user = await _userRepository.GetByIdAsync(patient.UserId, cancellationToken);
            patientName = user?.FullName;
        }

        var dto = new InvoiceDto(
            invoice.Id,
            invoice.TenantId,
            invoice.AppointmentId,
            invoice.PatientId,
            invoice.ClinicId,
            invoice.InvoiceNumber,
            invoice.SubTotal,
            invoice.DiscountAmount,
            invoice.TaxAmount,
            invoice.TotalAmount,
            invoice.PaidAmount,
            invoice.Status,
            invoice.PaymentMethod,
            invoice.PaidAtUtc,
            invoice.IssuedAtUtc,
            invoice.Items.Select(i => new InvoiceItemDto(i.Id, i.Description, i.Quantity, i.UnitPrice, i.TotalPrice)).ToList(),
            patientName,
            clinic?.NameEn);

        return Result.Success(dto);
    }
}
