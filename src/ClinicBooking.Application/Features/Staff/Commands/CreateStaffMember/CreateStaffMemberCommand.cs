using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Contracts.Services;
using ClinicBooking.Application.Features.Staff.DTOs;
using ClinicBooking.Domain.Constants;
using ClinicBooking.Domain.Entities;
using ClinicBooking.Domain.Enums;
using FluentValidation;
using MediatR;

namespace ClinicBooking.Application.Features.Staff.Commands.CreateStaffMember;

public sealed record CreateStaffMemberCommand(
    Guid? TenantId,
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string Role,
    Guid? ClinicId,
    IReadOnlyList<string>? Permissions,
    string? CallerRole = null) : IRequest<Result<StaffMemberDto>>;

public sealed class CreateStaffMemberCommandValidator : AbstractValidator<CreateStaffMemberCommand>
{
    public CreateStaffMemberCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Role).NotEmpty();
    }
}

public sealed class CreateStaffMemberCommandHandler : IRequestHandler<CreateStaffMemberCommand, Result<StaffMemberDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IClinicRepository _clinicRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public CreateStaffMemberCommandHandler(
        IUserRepository userRepository,
        IClinicRepository clinicRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _clinicRepository = clinicRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<StaffMemberDto>> Handle(CreateStaffMemberCommand request, CancellationToken cancellationToken)
    {
        if (await _userRepository.ExistsByEmailAsync(request.Email, cancellationToken))
        {
            return Result.Failure<StaffMemberDto>(Error.Conflict("User.EmailExists", "A user with this email address already exists."));
        }

        if (!Enum.TryParse<UserRole>(request.Role, true, out var role) || role == UserRole.Patient)
        {
            return Result.Failure<StaffMemberDto>(Error.Validation("User.InvalidRole", "Invalid staff role specified."));
        }

        // Enforce: Administrator is the only one responsible for adding Clinic Admin and Admin
        if (request.CallerRole == "ClinicAdmin" && (role == UserRole.Admin || role == UserRole.ClinicAdmin))
        {
            return Result.Failure<StaffMemberDto>(Error.Forbidden("Only system administrators can create clinic administrator or administrator accounts."));
        }

        string? clinicName = null;
        if (request.ClinicId.HasValue)
        {
            var clinic = await _clinicRepository.GetByIdAsync(request.ClinicId.Value, cancellationToken);
            if (clinic is null)
            {
                return Result.Failure<StaffMemberDto>(Error.NotFound("Clinic", request.ClinicId.Value));
            }
            clinicName = clinic.NameEn;
        }

        var user = User.Create(
            email: request.Email,
            firstName: request.FirstName,
            lastName: request.LastName,
            role: role,
            tenantId: request.TenantId,
            clinicId: request.ClinicId);

        user.SetPasswordHash(_passwordHasher.HashPassword(request.Password));

        // Assign explicit permissions if provided; otherwise preset based on role
        if (request.Permissions is not null && request.Permissions.Count > 0)
        {
            user.SetPermissions(request.Permissions);
        }
        else if (ClinicPermissions.RolePresets.TryGetValue(role.ToString(), out var preset))
        {
            user.SetPermissions(preset);
        }

        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
