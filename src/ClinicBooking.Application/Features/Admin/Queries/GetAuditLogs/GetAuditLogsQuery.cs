using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using MediatR;

namespace ClinicBooking.Application.Features.Admin.Queries.GetAuditLogs;

public sealed record AuditLogDto(
    Guid Id,
    Guid? TenantId,
    string EntityName,
    string EntityId,
    string Action,
    string ChangedBy,
    DateTime ChangedAtUtc,
    string? ChangesJson);

public sealed record GetAuditLogsQuery(
    Guid? TenantId,
    string? EntityName = null,
    int Limit = 100) : IRequest<Result<IReadOnlyList<AuditLogDto>>>;

public sealed class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, Result<IReadOnlyList<AuditLogDto>>>
{
    private readonly IAuditLogRepository _auditLogRepository;

    public GetAuditLogsQueryHandler(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    public async Task<Result<IReadOnlyList<AuditLogDto>>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var logs = await _auditLogRepository.ListAsync(request.TenantId, request.EntityName, request.Limit, cancellationToken);
        var dtos = logs.Select(l => new AuditLogDto(
            l.Id,
            l.TenantId,
            l.EntityName,
            l.EntityId,
            l.Action,
            l.ChangedBy,
            l.ChangedAtUtc,
            l.ChangesJson)).ToList();

        return Result.Success<IReadOnlyList<AuditLogDto>>(dtos);
    }
}
