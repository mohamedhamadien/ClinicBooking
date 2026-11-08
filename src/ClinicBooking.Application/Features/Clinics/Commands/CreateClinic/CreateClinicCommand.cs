using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Features.Clinics.DTOs;
using ClinicBooking.Domain.Entities;
using FluentValidation;
using MediatR;

namespace ClinicBooking.Application.Features.Clinics.Commands.CreateClinic;

public sealed record CreateClinicCommand(
    Guid TenantId,
    string Name,
    string Address,
    string City,
    string PhoneNumber) : IRequest<Result<ClinicDto>>;

public sealed class CreateClinicCommandValidator : AbstractValidator<CreateClinicCommand>
{
    public CreateClinicCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty().WithMessage("TenantId is required.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(200);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PhoneNumber).NotEmpty();
    }
}

public sealed class CreateClinicCommandHandler : IRequestHandler<CreateClinicCommand, Result<ClinicDto>>
{
    private readonly IClinicRepository _clinicRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateClinicCommandHandler(
        IClinicRepository clinicRepository,
        ITenantRepository tenantRepository,
        IUnitOfWork unitOfWork)
    {
        _clinicRepository = clinicRepository;
        _tenantRepository = tenantRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ClinicDto>> Handle(CreateClinicCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);
        if (tenant is null || !tenant.IsActive)
        {
            return Result.Failure<ClinicDto>(Error.NotFound("Tenant", request.TenantId));
        }

        var clinic = Clinic.Create(
            request.TenantId,
            request.Name,
            request.Address,
            request.City,
            request.PhoneNumber);

        await _clinicRepository.AddAsync(clinic, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new ClinicDto(
            clinic.Id,
            clinic.TenantId,
            clinic.Name,
            clinic.Address,
            clinic.City,
            clinic.PhoneNumber.Value,
            clinic.IsActive);

        return Result.Success(dto);
    }
}
