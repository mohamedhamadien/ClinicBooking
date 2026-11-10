namespace ClinicBooking.Application.Features.Inventory.DTOs;

public sealed record MedicalInventoryItemDto(
    Guid Id,
    Guid ClinicId,
    string? ClinicName,
    string ItemName,
    string? ItemNameAr,
    string Category,
    int CurrentStock,
    int MinimumThreshold,
    string Unit,
    decimal UnitPrice,
    string? BatchNumber,
    DateTime? ExpiryDate,
    bool IsLowStock,
    bool IsExpired,
    DateTime LastUpdatedUtc);
