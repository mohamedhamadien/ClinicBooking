using ClinicBooking.Domain.Common;
using ClinicBooking.Domain.Enums;
using ClinicBooking.Domain.Exceptions;

namespace ClinicBooking.Domain.Entities;

public sealed class Invoice : AggregateRoot<Guid>, IAuditableEntity, IMustHaveTenant
{
    public Guid TenantId { get; private set; }
    public Guid? AppointmentId { get; private set; }
    public Guid PatientId { get; private set; }
    public Guid ClinicId { get; private set; }
    public string InvoiceNumber { get; private set; } = default!;
    public decimal SubTotal { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal PaidAmount { get; private set; }
    public InvoiceStatus Status { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    public Guid? InsuranceProviderId { get; private set; }
    public InsuranceProvider? InsuranceProvider { get; private set; }
    public decimal InsuranceCoverageAmount { get; private set; }
    public decimal PatientCopayAmount { get; private set; }
    public DateTime? PaidAtUtc { get; private set; }
    public DateTime IssuedAtUtc { get; private set; }

    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModifiedAtUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    private readonly List<InvoiceItem> _items = [];
    public IReadOnlyCollection<InvoiceItem> Items => _items.AsReadOnly();

    private Invoice() { }

    public Invoice(
        Guid id,
        Guid tenantId,
        Guid? appointmentId,
        Guid patientId,
        Guid clinicId,
        string invoiceNumber,
        decimal discountAmount,
        decimal taxAmount,
        PaymentMethod paymentMethod,
        DateTime issuedAtUtc) : base(id)
    {
        TenantId = tenantId;
        AppointmentId = appointmentId;
        PatientId = patientId;
        ClinicId = clinicId;
        InvoiceNumber = string.IsNullOrWhiteSpace(invoiceNumber)
            ? $"INV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}"
            : invoiceNumber.Trim();
        DiscountAmount = discountAmount >= 0 ? discountAmount : 0;
        TaxAmount = taxAmount >= 0 ? taxAmount : 0;
        PaymentMethod = paymentMethod;
        Status = InvoiceStatus.Pending;
        IssuedAtUtc = DateTime.SpecifyKind(issuedAtUtc, DateTimeKind.Utc);
    }

    public static Invoice Create(
        Guid tenantId,
        Guid? appointmentId,
        Guid patientId,
        Guid clinicId,
        decimal discountAmount = 0,
        decimal taxAmount = 0,
        PaymentMethod paymentMethod = PaymentMethod.Cash,
        string? invoiceNumber = null)
    {
        return new Invoice(
            Guid.NewGuid(),
            tenantId,
            appointmentId,
            patientId,
            clinicId,
            invoiceNumber ?? string.Empty,
            discountAmount,
            taxAmount,
            paymentMethod,
            DateTime.UtcNow);
    }

    public void AddItem(string description, int quantity, decimal unitPrice)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new DomainValidationException("Invoice line item description is required.");
        }

        var item = new InvoiceItem(Guid.NewGuid(), Id, description, quantity, unitPrice);
        _items.Add(item);

        RecalculateTotals();
    }

    public void MarkAsPaid(PaymentMethod paymentMethod, DateTime paidAtUtc)
    {
        PaymentMethod = paymentMethod;
        PaidAmount = TotalAmount;
        Status = InvoiceStatus.Paid;
        PaidAtUtc = DateTime.SpecifyKind(paidAtUtc, DateTimeKind.Utc);
    }

    public void Cancel()
    {
        Status = InvoiceStatus.Cancelled;
    }

    public void SetInsuranceDetails(Guid insuranceProviderId, decimal coverageAmount, decimal copayAmount)
    {
        InsuranceProviderId = insuranceProviderId;
        InsuranceCoverageAmount = Math.Max(0, coverageAmount);
        PatientCopayAmount = Math.Max(0, copayAmount);
    }

    private void RecalculateTotals()
    {
        SubTotal = _items.Sum(i => i.TotalPrice);
        TotalAmount = Math.Max(0, SubTotal - DiscountAmount + TaxAmount);
    }
}
