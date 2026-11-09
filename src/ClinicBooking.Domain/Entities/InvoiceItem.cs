using ClinicBooking.Domain.Common;

namespace ClinicBooking.Domain.Entities;

public sealed class InvoiceItem : Entity<Guid>
{
    public Guid InvoiceId { get; private set; }
    public string Description { get; private set; } = default!;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal TotalPrice => Quantity * UnitPrice;

    private InvoiceItem() { }

    public InvoiceItem(Guid id, Guid invoiceId, string description, int quantity, decimal unitPrice) : base(id)
    {
        InvoiceId = invoiceId;
        Description = description.Trim();
        Quantity = quantity > 0 ? quantity : 1;
        UnitPrice = unitPrice >= 0 ? unitPrice : 0;
    }
}
