using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Features.Inventory.DTOs;
using MediatR;

namespace ClinicBooking.Application.Features.Inventory.Queries.GetClinicInventory;

public sealed record GetClinicInventoryQuery(Guid? ClinicId = null) : IRequest<Result<IReadOnlyList<MedicalInventoryItemDto>>>;

public sealed class GetClinicInventoryQueryHandler : IRequestHandler<GetClinicInventoryQuery, Result<IReadOnlyList<MedicalInventoryItemDto>>>
{
    private readonly IMedicalInventoryRepository _inventoryRepository;

    public GetClinicInventoryQueryHandler(IMedicalInventoryRepository inventoryRepository)
    {
        _inventoryRepository = inventoryRepository;
    }

    public async Task<Result<IReadOnlyList<MedicalInventoryItemDto>>> Handle(GetClinicInventoryQuery request, CancellationToken cancellationToken)
    {
        var items = request.ClinicId.HasValue && request.ClinicId.Value != Guid.Empty
            ? await _inventoryRepository.GetByClinicIdAsync(request.ClinicId.Value, cancellationToken)
            : await _inventoryRepository.GetAllAsync(cancellationToken);

        var dtos = items.Select(i => new MedicalInventoryItemDto(
            i.Id,
            i.ClinicId,
            i.Clinic?.NameEn,
            i.ItemName,
            i.ItemNameAr,
            i.Category,
            i.CurrentStock,
            i.MinimumThreshold,
            i.Unit,
            i.UnitPrice,
            i.BatchNumber,
            i.ExpiryDate,
            i.IsLowStock,
            i.IsExpired,
            i.LastUpdatedUtc)).ToList();

        return Result.Success<IReadOnlyList<MedicalInventoryItemDto>>(dtos);
    }
}
