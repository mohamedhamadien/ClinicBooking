using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Features.Staff.DTOs;
using ClinicBooking.Domain.Enums;
using MediatR;

namespace ClinicBooking.Application.Features.Staff.Commands.ToggleStaffStatus;

public sealed record ToggleStaffStatusCommand(
    Guid StaffUserId,
    string? CallerRole = null,
    Guid? CallerClinicId = null) : IRequest<Result<StaffMemberDto>>;

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

        // Enforce: Clinic Admin cannot deactivate/activate administrator accounts or staff from other clinics
        if (request.CallerRole == "ClinicAdmin")
        {
            if (user.Role == UserRole.Admin || user.Role == UserRole.ClinicAdmin)
            {
                return Result.Failure<StaffMemberDto>(Error.Forbidden("Clinic administrators cannot modify administrator accounts."));
            }

            if (request.CallerClinicId.HasValue && user.ClinicId != request.CallerClinicId)
            {
                return Result.Failure<StaffMemberDto>(Error.Forbidden("Clinic administrators can only manage staff belonging to their assigned clinic."));
            }
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
