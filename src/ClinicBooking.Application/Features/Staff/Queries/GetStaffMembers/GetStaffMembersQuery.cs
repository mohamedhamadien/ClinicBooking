using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Features.Staff.DTOs;
using MediatR;

namespace ClinicBooking.Application.Features.Staff.Queries.GetStaffMembers;

public sealed record GetStaffMembersQuery(Guid? TenantId, Guid? ClinicId = null) : IRequest<Result<IReadOnlyList<StaffMemberDto>>>;

public sealed class GetStaffMembersQueryHandler : IRequestHandler<GetStaffMembersQuery, Result<IReadOnlyList<StaffMemberDto>>>
{
    private readonly IUserRepository _userRepository;
    private readonly IClinicRepository _clinicRepository;

    public GetStaffMembersQueryHandler(IUserRepository userRepository, IClinicRepository clinicRepository)
    {
        _userRepository = userRepository;
        _clinicRepository = clinicRepository;
    }

    public async Task<Result<IReadOnlyList<StaffMemberDto>>> Handle(GetStaffMembersQuery request, CancellationToken cancellationToken)
    {
        var staffUsers = await _userRepository.GetStaffMembersAsync(request.TenantId, request.ClinicId, cancellationToken);

        // Fetch clinic names to enrich the DTO
        var clinicIds = staffUsers.Where(u => u.ClinicId.HasValue).Select(u => u.ClinicId!.Value).Distinct().ToList();
        var clinicNameMap = new Dictionary<Guid, string>();
        foreach (var cId in clinicIds)
        {
            var clinic = await _clinicRepository.GetByIdAsync(cId, cancellationToken);
            if (clinic is not null)
            {
                clinicNameMap[cId] = clinic.NameEn;
            }
        }

        var dtos = staffUsers.Select(u => new StaffMemberDto(
            u.Id,
            u.Email.Value,
            u.FirstName,
            u.LastName,
            u.FullName,
            u.Role.ToString(),
            u.IsActive,
            u.ClinicId,
            u.ClinicId.HasValue && clinicNameMap.TryGetValue(u.ClinicId.Value, out var clinicName) ? clinicName : null,
            u.GetEffectivePermissions(),
            u.CreatedAtUtc)).ToList();

        return Result.Success<IReadOnlyList<StaffMemberDto>>(dtos);
    }
}
