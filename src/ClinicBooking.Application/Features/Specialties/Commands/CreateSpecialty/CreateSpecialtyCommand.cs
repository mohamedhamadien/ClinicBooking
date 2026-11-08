using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Features.Specialties.DTOs;
using ClinicBooking.Domain.Entities;
using FluentValidation;
using MediatR;

namespace ClinicBooking.Application.Features.Specialties.Commands.CreateSpecialty;

public sealed record CreateSpecialtyCommand(
    string Name,
    string Description,
    Guid? TenantId = null) : IRequest<Result<SpecialtyDto>>;

public sealed class CreateSpecialtyCommandValidator : AbstractValidator<CreateSpecialtyCommand>
{
    public CreateSpecialtyCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public sealed class CreateSpecialtyCommandHandler : IRequestHandler<CreateSpecialtyCommand, Result<SpecialtyDto>>
{
    private readonly ISpecialtyRepository _specialtyRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateSpecialtyCommandHandler(ISpecialtyRepository specialtyRepository, IUnitOfWork unitOfWork)
    {
        _specialtyRepository = specialtyRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<SpecialtyDto>> Handle(CreateSpecialtyCommand request, CancellationToken cancellationToken)
    {
        var specialty = Specialty.Create(request.Name, request.Description, request.TenantId);

        await _specialtyRepository.AddAsync(specialty, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new SpecialtyDto(
            specialty.Id,
            specialty.TenantId,
            specialty.Name,
            specialty.Description,
            specialty.IsActive);

        return Result.Success(dto);
    }
}
