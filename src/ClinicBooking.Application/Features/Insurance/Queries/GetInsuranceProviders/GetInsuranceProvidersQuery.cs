using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Features.Insurance.DTOs;
using MediatR;

namespace ClinicBooking.Application.Features.Insurance.Queries.GetInsuranceProviders;

public sealed record GetInsuranceProvidersQuery() : IRequest<Result<IReadOnlyList<InsuranceProviderDto>>>;

public sealed class GetInsuranceProvidersQueryHandler : IRequestHandler<GetInsuranceProvidersQuery, Result<IReadOnlyList<InsuranceProviderDto>>>
{
    private readonly IInsuranceProviderRepository _insuranceRepository;

    public GetInsuranceProvidersQueryHandler(IInsuranceProviderRepository insuranceRepository)
    {
        _insuranceRepository = insuranceRepository;
    }

    public async Task<Result<IReadOnlyList<InsuranceProviderDto>>> Handle(GetInsuranceProvidersQuery request, CancellationToken cancellationToken)
    {
        var providers = await _insuranceRepository.GetAllAsync(cancellationToken);
        var dtos = providers.Select(p => new InsuranceProviderDto(
            p.Id,
            p.Name,
            p.NameAr,
            p.Code,
            p.DefaultCoveragePercentage,
            p.ContactPhone,
            p.IsActive)).ToList();

        return Result.Success<IReadOnlyList<InsuranceProviderDto>>(dtos);
    }
}
