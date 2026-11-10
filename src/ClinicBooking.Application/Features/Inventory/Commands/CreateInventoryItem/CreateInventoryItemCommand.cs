using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Features.Inventory.DTOs;
using ClinicBooking.Domain.Entities;
using FluentValidation;
using MediatR;

namespace ClinicBooking.Application.Features.Inventory.Commands.CreateInventoryItem;

public sealed record CreateInventoryItemCommand(
    Guid ClinicId,
    string ItemName,
    string? ItemNameAr,
    string Category,
    int InitialStock,
    int MinimumThreshold,
    string Unit,
    decimal UnitPrice,
    string? BatchNumber,
    DateTime? ExpiryDate,
    Guid? TenantId = null) : IRequest<Result<MedicalInventoryItemDto>>;

public sealed class CreateInventoryItemCommandValidator : AbstractValidator<CreateInventoryItemCommand>
{
    public CreateInventoryItemCommandValidator()
    {
        RuleFor(x => x.ClinicId).NotEmpty();
        RuleFor(x => x.ItemName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.InitialStock).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MinimumThreshold).GreaterThanOrEqualTo(0);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreateInventoryItemCommandHandler : IRequestHandler<CreateInventoryItemCommand, Result<MedicalInventoryItemDto>>
{
    private readonly IMedicalInventoryRepository _inventoryRepository;
    private readonly IClinicRepository _clinicRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateInventoryItemCommandHandler(
        IMedicalInventoryRepository inventoryRepository,
        IClinicRepository clinicRepository,
        IUnitOfWork unitOfWork)
    {
        _inventoryRepository = inventoryRepository;
        _clinicRepository = clinicRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<MedicalInventoryItemDto>> Handle(CreateInventoryItemCommand request, CancellationToken cancellationToken)
    {
        var clinic = await _clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);
        if (clinic is null)
        {
            return Result.Failure<MedicalInventoryItemDto>(Error.NotFound("Clinic.NotFound", "The clinic facility was not found."));
        }

        var item = MedicalInventoryItem.Create(
            request.ClinicId,
            request.ItemName,
            request.ItemNameAr,
            request.Category,
            request.InitialStock,
            request.MinimumThreshold,
            request.Unit,
            request.UnitPrice,
            request.BatchNumber,
            request.ExpiryDate,
            request.TenantId ?? clinic.TenantId);

        await _inventoryRepository.AddAsync(item, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new MedicalInventoryItemDto(
            item.Id,
            item.ClinicId,
            clinic.NameEn,
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
