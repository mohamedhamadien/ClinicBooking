using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Features.Invoices.DTOs;
using ClinicBooking.Domain.Entities;
using ClinicBooking.Domain.Enums;
using FluentValidation;
using MediatR;

namespace ClinicBooking.Application.Features.Invoices.Commands.CreateInvoice;

public sealed record InvoiceLineItemInput(
    string Description,
    int Quantity,
    decimal UnitPrice);

public sealed record CreateInvoiceCommand(
    Guid TenantId,
    Guid? AppointmentId,
    Guid PatientId,
    Guid ClinicId,
    decimal DiscountAmount = 0,
    decimal TaxAmount = 0,
    PaymentMethod PaymentMethod = PaymentMethod.Cash,
    IReadOnlyList<InvoiceLineItemInput>? Items = null) : IRequest<Result<InvoiceDto>>;

public sealed class CreateInvoiceCommandValidator : AbstractValidator<CreateInvoiceCommand>
{
    public CreateInvoiceCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.ClinicId).NotEmpty();
        RuleFor(x => x.DiscountAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TaxAmount).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreateInvoiceCommandHandler : IRequestHandler<CreateInvoiceCommand, Result<InvoiceDto>>
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IClinicRepository _clinicRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateInvoiceCommandHandler(
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

    public async Task<Result<InvoiceDto>> Handle(CreateInvoiceCommand request, CancellationToken cancellationToken)
    {
        var invoice = Invoice.Create(
            request.TenantId,
            request.AppointmentId,
            request.PatientId,
            request.ClinicId,
            request.DiscountAmount,
            request.TaxAmount,
            request.PaymentMethod);

        if (request.Items is not null)
        {
            foreach (var item in request.Items)
            {
                invoice.AddItem(item.Description, item.Quantity, item.UnitPrice);
            }
        }

        await _invoiceRepository.AddAsync(invoice, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var clinic = await _clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);
        var patient = await _patientRepository.GetByIdAsync(request.PatientId, cancellationToken);
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
