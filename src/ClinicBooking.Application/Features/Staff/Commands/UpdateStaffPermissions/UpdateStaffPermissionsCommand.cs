using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Features.Staff.DTOs;
using ClinicBooking.Domain.Entities;
using ClinicBooking.Domain.Enums;
using FluentValidation;
using MediatR;

namespace ClinicBooking.Application.Features.Staff.Commands.UpdateStaffPermissions;

public sealed record UpdateStaffPermissionsCommand(
    Guid StaffUserId,
    string? Role,
    Guid? ClinicId,
    IReadOnlyList<string> Permissions,
    string? CallerRole = null,
    Guid? CallerClinicId = null) : IRequest<Result<StaffMemberDto>>;

public sealed class UpdateStaffPermissionsCommandValidator : AbstractValidator<UpdateStaffPermissionsCommand>
{
    public UpdateStaffPermissionsCommandValidator()
    {
        RuleFor(x => x.StaffUserId).NotEmpty();
        RuleFor(x => x.Permissions).NotNull();
    }
}

public sealed class UpdateStaffPermissionsCommandHandler : IRequestHandler<UpdateStaffPermissionsCommand, Result<StaffMemberDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IClinicRepository _clinicRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateStaffPermissionsCommandHandler(
        IUserRepository userRepository,
        IClinicRepository clinicRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _clinicRepository = clinicRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<StaffMemberDto>> Handle(UpdateStaffPermissionsCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.StaffUserId, cancellationToken);
        if (user is null || user.Role == UserRole.Patient)
        {
            return Result.Failure<StaffMemberDto>(Error.NotFound("User", request.StaffUserId));
        }

        // Enforce: Clinic Admin can manage staffs (Receptionist, Nurse, Accountant, Doctor, Staff) in their clinic
        // but cannot modify Admin/ClinicAdmin or promote staff to Admin/ClinicAdmin
        if (request.CallerRole == "ClinicAdmin")
        {
            if (user.Role == UserRole.Admin || user.Role == UserRole.ClinicAdmin)
            {
                return Result.Failure<StaffMemberDto>(Error.Forbidden("Clinic administrators cannot modify administrator accounts."));
            }

            if (!string.IsNullOrWhiteSpace(request.Role) &&
                (string.Equals(request.Role, "Admin", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(request.Role, "ClinicAdmin", StringComparison.OrdinalIgnoreCase)))
            {
                return Result.Failure<StaffMemberDto>(Error.Forbidden("Only system administrators can assign clinic administrator or administrator roles."));
            }

            if (request.CallerClinicId.HasValue && user.ClinicId != request.CallerClinicId)
            {
                return Result.Failure<StaffMemberDto>(Error.Forbidden("Clinic administrators can only manage staff belonging to their assigned clinic."));
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            if (Enum.TryParse<UserRole>(request.Role, true, out var newRole) && newRole != UserRole.Patient)
            {
                user.SetRole(newRole);
            }
        }

        if (request.ClinicId.HasValue)
        {
            user.AssignToClinic(request.ClinicId.Value);
        }

        user.SetPermissions(request.Permissions);

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
