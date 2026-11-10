using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using MediatR;

namespace ClinicBooking.Application.Features.Inventory.Commands.DeleteInventoryItem;

public sealed record DeleteInventoryItemCommand(Guid Id) : IRequest<Result<Unit>>;

public sealed class DeleteInventoryItemCommandHandler : IRequestHandler<DeleteInventoryItemCommand, Result<Unit>>
{
    private readonly IMedicalInventoryRepository _inventoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteInventoryItemCommandHandler(
        IMedicalInventoryRepository inventoryRepository,
        IUnitOfWork unitOfWork)
    {
        _inventoryRepository = inventoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Unit>> Handle(DeleteInventoryItemCommand request, CancellationToken cancellationToken)
    {
        var item = await _inventoryRepository.GetByIdAsync(request.Id, cancellationToken);
        if (item is null)
        {
            return Result.Failure<Unit>(Error.NotFound("Inventory.NotFound", "The inventory item was not found."));
        }

        _inventoryRepository.Delete(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(Unit.Value);
    }
}
