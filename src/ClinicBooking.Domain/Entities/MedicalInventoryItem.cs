using ClinicBooking.Domain.Common;

namespace ClinicBooking.Domain.Entities;

public sealed class MedicalInventoryItem : Entity<Guid>, IMayHaveTenant
{
    private MedicalInventoryItem() { }

    public Guid ClinicId { get; private set; }
    public Clinic? Clinic { get; private set; }

    public string ItemName { get; private set; } = string.Empty;
    public string? ItemNameAr { get; private set; }
    public string Category { get; private set; } = "Consumables"; // Consumables, Pharmaceuticals, Equipment, Supplies
    public int CurrentStock { get; private set; }
    public int MinimumThreshold { get; private set; } = 10;
    public string Unit { get; private set; } = "Box"; // Box, Piece, Ampoule, Bottle, Pack, Vial
    public decimal UnitPrice { get; private set; }
    public string? BatchNumber { get; private set; }
    public DateTime? ExpiryDate { get; private set; }
    public DateTime LastUpdatedUtc { get; private set; }
    public Guid? TenantId { get; private set; }

    public bool IsLowStock => CurrentStock <= MinimumThreshold;
    public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value.Date < DateTime.UtcNow.Date;

    public static MedicalInventoryItem Create(
        Guid clinicId,
        string itemName,
        string? itemNameAr,
        string category,
        int initialStock,
        int minimumThreshold,
        string unit,
        decimal unitPrice,
        string? batchNumber,
        DateTime? expiryDate,
        Guid? tenantId = null)
    {
        return new MedicalInventoryItem
        {
            Id = Guid.NewGuid(),
            ClinicId = clinicId,
            ItemName = itemName,
            ItemNameAr = itemNameAr,
            Category = string.IsNullOrWhiteSpace(category) ? "Consumables" : category,
            CurrentStock = Math.Max(0, initialStock),
            MinimumThreshold = Math.Max(0, minimumThreshold),
            Unit = string.IsNullOrWhiteSpace(unit) ? "Piece" : unit,
            UnitPrice = Math.Max(0, unitPrice),
            BatchNumber = batchNumber,
            ExpiryDate = expiryDate,
            LastUpdatedUtc = DateTime.UtcNow,
            TenantId = tenantId
        };
    }

    public void AdjustStock(int deltaQuantity)
    {
        CurrentStock = Math.Max(0, CurrentStock + deltaQuantity);
        LastUpdatedUtc = DateTime.UtcNow;
    }

    public void UpdateDetails(
        string itemName,
        string? itemNameAr,
        string category,
        int minimumThreshold,
        string unit,
        decimal unitPrice,
        string? batchNumber,
        DateTime? expiryDate)
    {
        ItemName = itemName;
        ItemNameAr = itemNameAr;
        Category = string.IsNullOrWhiteSpace(category) ? Category : category;
        MinimumThreshold = Math.Max(0, minimumThreshold);
        Unit = string.IsNullOrWhiteSpace(unit) ? Unit : unit;
        UnitPrice = Math.Max(0, unitPrice);
        BatchNumber = batchNumber;
        ExpiryDate = expiryDate;
        LastUpdatedUtc = DateTime.UtcNow;
    }
}
