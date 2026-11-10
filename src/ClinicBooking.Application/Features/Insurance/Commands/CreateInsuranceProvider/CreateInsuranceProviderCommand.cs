using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Features.Insurance.DTOs;
using ClinicBooking.Domain.Entities;
using FluentValidation;
using MediatR;

namespace ClinicBooking.Application.Features.Insurance.Commands.CreateInsuranceProvider;

public sealed record CreateInsuranceProviderCommand(
    string Name,
    string? NameAr,
    string Code,
    decimal DefaultCoveragePercentage = 80m,
    string? ContactPhone = null,
    Guid? TenantId = null) : IRequest<Result<InsuranceProviderDto>>;

public sealed class CreateInsuranceProviderCommandValidator : AbstractValidator<CreateInsuranceProviderCommand>
{
    public CreateInsuranceProviderCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.DefaultCoveragePercentage).InclusiveBetween(0m, 100m);
    }
}

public sealed class CreateInsuranceProviderCommandHandler : IRequestHandler<CreateInsuranceProviderCommand, Result<InsuranceProviderDto>>
{
    private readonly IInsuranceProviderRepository _insuranceRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateInsuranceProviderCommandHandler(
        IInsuranceProviderRepository insuranceRepository,
        IUnitOfWork unitOfWork)
    {
        _insuranceRepository = insuranceRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<InsuranceProviderDto>> Handle(CreateInsuranceProviderCommand request, CancellationToken cancellationToken)
    {
        var provider = InsuranceProvider.Create(
            request.Name,
            request.NameAr,
            request.Code,
            request.DefaultCoveragePercentage,
            request.ContactPhone,
            request.TenantId);

        await _insuranceRepository.AddAsync(provider, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new InsuranceProviderDto(
            provider.Id,
            provider.Name,
            provider.NameAr,
            provider.Code,
            provider.DefaultCoveragePercentage,
            provider.ContactPhone,
            provider.IsActive);

        return Result.Success(dto);
    }
}
