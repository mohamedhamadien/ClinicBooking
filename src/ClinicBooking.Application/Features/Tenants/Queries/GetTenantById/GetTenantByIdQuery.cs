using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Features.Tenants.DTOs;
using MediatR;

namespace ClinicBooking.Application.Features.Tenants.Queries.GetTenantById;

public sealed record GetTenantByIdQuery(Guid TenantId) : IRequest<Result<TenantDto>>;

public sealed class GetTenantByIdQueryHandler : IRequestHandler<GetTenantByIdQuery, Result<TenantDto>>
{
    private readonly ITenantRepository _tenantRepository;

    public GetTenantByIdQueryHandler(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    public async Task<Result<TenantDto>> Handle(GetTenantByIdQuery request, CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);
        if (tenant is null)
        {
            return Result.Failure<TenantDto>(Error.NotFound("Tenant", request.TenantId));
        }

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
