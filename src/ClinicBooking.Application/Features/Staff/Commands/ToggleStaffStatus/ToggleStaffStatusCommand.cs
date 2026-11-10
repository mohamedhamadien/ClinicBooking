using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Features.Staff.DTOs;
using ClinicBooking.Domain.Enums;
using MediatR;

namespace ClinicBooking.Application.Features.Staff.Commands.ToggleStaffStatus;

public sealed record ToggleStaffStatusCommand(Guid StaffUserId) : IRequest<Result<StaffMemberDto>>;

public sealed class ToggleStaffStatusCommandHandler : IRequestHandler<ToggleStaffStatusCommand, Result<StaffMemberDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IClinicRepository _clinicRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ToggleStaffStatusCommandHandler(
        IUserRepository userRepository,
        IClinicRepository clinicRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _clinicRepository = clinicRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<StaffMemberDto>> Handle(ToggleStaffStatusCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.StaffUserId, cancellationToken);
        if (user is null || user.Role == UserRole.Patient)
        {
            return Result.Failure<StaffMemberDto>(Error.NotFound("User", request.StaffUserId));
        }

        if (user.IsActive)
        {
            user.Deactivate();
        }
        else
        {
            user.Activate();
        }

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        string? clinicName = null;
        if (user.ClinicId.HasValue)
        {
            var clinic = await _clinicRepository.GetByIdAsync(user.ClinicId.Value, cancellationToken);
            clinicName = clinic?.NameEn;
        }

        var dto = new StaffMemberDto(
            user.Id,
            user.Email.Value,
            user.FirstName,
            user.LastName,
            user.FullName,
            user.Role.ToString(),
            user.IsActive,
            user.ClinicId,
            clinicName,
            user.GetEffectivePermissions(),
            user.CreatedAtUtc);

        return Result.Success(dto);
    }
}
