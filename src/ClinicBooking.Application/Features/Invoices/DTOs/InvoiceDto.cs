using ClinicBooking.Domain.Enums;

namespace ClinicBooking.Application.Features.Invoices.DTOs;

public sealed record InvoiceItemDto(
    Guid Id,
    string Description,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice);

public sealed record InvoiceDto(
    Guid Id,
    Guid TenantId,
    Guid? AppointmentId,
    Guid PatientId,
    Guid ClinicId,
    string InvoiceNumber,
    decimal SubTotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    decimal PaidAmount,
    InvoiceStatus Status,
    PaymentMethod PaymentMethod,
    DateTime? PaidAtUtc,
    DateTime IssuedAtUtc,
    IReadOnlyList<InvoiceItemDto> Items,
    string? PatientName = null,
    string? ClinicName = null,
    Guid? InsuranceProviderId = null,
    string? InsuranceProviderName = null,
    decimal InsuranceCoverageAmount = 0,
    decimal PatientCopayAmount = 0)
{
    public decimal RemainingAmount => Math.Max(0, TotalAmount - PaidAmount);
}
