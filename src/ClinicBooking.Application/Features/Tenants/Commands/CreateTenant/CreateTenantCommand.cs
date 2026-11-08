using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Features.Tenants.DTOs;
using ClinicBooking.Domain.Entities;
using FluentValidation;
using MediatR;

namespace ClinicBooking.Application.Features.Tenants.Commands.CreateTenant;

public sealed record CreateTenantCommand(
    string Name,
    string Identifier,
    int CancellationNoticeHours = 24,
    bool RequiresDoctorApproval = false) : IRequest<Result<TenantDto>>;

public sealed class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    public CreateTenantCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tenant name is required.")
            .MaximumLength(150);

        RuleFor(x => x.Identifier)
            .NotEmpty().WithMessage("Tenant identifier is required.")
            .MinimumLength(3)
            .MaximumLength(50)
            .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$")
            .WithMessage("Identifier must be lowercase alphanumeric characters and hyphens.");

        RuleFor(x => x.CancellationNoticeHours)
            .InclusiveBetween(0, 168)
            .WithMessage("Cancellation notice hours must be between 0 and 168 (7 days).");
    }
}

public sealed class CreateTenantCommandHandler : IRequestHandler<CreateTenantCommand, Result<TenantDto>>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateTenantCommandHandler(ITenantRepository tenantRepository, IUnitOfWork unitOfWork)
    {
        _tenantRepository = tenantRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<TenantDto>> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        if (await _tenantRepository.ExistsAsync(request.Identifier, cancellationToken))
        {
            return Result.Failure<TenantDto>(Error.Conflict("Tenant.DuplicateIdentifier",
                $"A tenant with identifier '{request.Identifier}' already exists."));
        }

        var tenant = Tenant.Create(
            request.Name,
            request.Identifier,
            request.CancellationNoticeHours,
            request.RequiresDoctorApproval);

        await _tenantRepository.AddAsync(tenant, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new TenantDto(
            tenant.Id,
            tenant.Name,
            tenant.Identifier,
            tenant.IsActive,
            tenant.CancellationNoticeHours,
            tenant.RequiresDoctorApproval);

        return Result.Success(dto);
    }
}
