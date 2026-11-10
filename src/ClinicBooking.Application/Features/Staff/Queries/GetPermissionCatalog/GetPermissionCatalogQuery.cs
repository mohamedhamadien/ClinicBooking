using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Features.Staff.DTOs;
using ClinicBooking.Domain.Constants;
using MediatR;

namespace ClinicBooking.Application.Features.Staff.Queries.GetPermissionCatalog;

public sealed record GetPermissionCatalogQuery() : IRequest<Result<PermissionCatalogDto>>;

public sealed class GetPermissionCatalogQueryHandler : IRequestHandler<GetPermissionCatalogQuery, Result<PermissionCatalogDto>>
{
    public Task<Result<PermissionCatalogDto>> Handle(GetPermissionCatalogQuery request, CancellationToken cancellationToken)
    {
        var items = ClinicPermissions.All.Select(p => new PermissionItemDto(
            p.Key,
            p.NameEn,
            p.NameAr,
            p.Category,
            p.Description)).ToList();

        var catalog = new PermissionCatalogDto(items, ClinicPermissions.RolePresets);
        return Task.FromResult(Result.Success(catalog));
    }
}
