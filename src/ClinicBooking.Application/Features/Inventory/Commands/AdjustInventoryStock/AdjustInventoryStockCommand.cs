using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Features.Inventory.DTOs;
using MediatR;

namespace ClinicBooking.Application.Features.Inventory.Commands.AdjustInventoryStock;

public sealed record AdjustInventoryStockCommand(Guid Id, int DeltaQuantity) : IRequest<Result<MedicalInventoryItemDto>>;

public sealed class AdjustInventoryStockCommandHandler : IRequestHandler<AdjustInventoryStockCommand, Result<MedicalInventoryItemDto>>
{
    private readonly IMedicalInventoryRepository _inventoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AdjustInventoryStockCommandHandler(
        IMedicalInventoryRepository inventoryRepository,
        IUnitOfWork unitOfWork)
    {
        _inventoryRepository = inventoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<MedicalInventoryItemDto>> Handle(AdjustInventoryStockCommand request, CancellationToken cancellationToken)
    {
        var item = await _inventoryRepository.GetByIdAsync(request.Id, cancellationToken);
        if (item is null)
        {
            return Result.Failure<MedicalInventoryItemDto>(Error.NotFound("Inventory.NotFound", "The inventory item was not found."));
        }

        item.AdjustStock(request.DeltaQuantity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new MedicalInventoryItemDto(
            item.Id,
            item.ClinicId,
            item.Clinic?.NameEn,
            item.ItemName,
            item.ItemNameAr,
            item.Category,
            item.CurrentStock,
            item.MinimumThreshold,
            item.Unit,
            item.UnitPrice,
            item.BatchNumber,
            item.ExpiryDate,
            item.IsLowStock,
            item.IsExpired,
            item.LastUpdatedUtc);

        return Result.Success(dto);
    }
}
