using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Features.Invoices.DTOs;
using MediatR;

namespace ClinicBooking.Application.Features.Invoices.Queries.GetInvoices;

public sealed record GetInvoicesQuery(Guid TenantId, Guid? ClinicId = null) : IRequest<Result<IReadOnlyList<InvoiceDto>>>;

public sealed class GetInvoicesQueryHandler : IRequestHandler<GetInvoicesQuery, Result<IReadOnlyList<InvoiceDto>>>
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IClinicRepository _clinicRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly IUserRepository _userRepository;

    public GetInvoicesQueryHandler(
        IInvoiceRepository invoiceRepository,
        IClinicRepository clinicRepository,
        IPatientRepository patientRepository,
        IUserRepository userRepository)
    {
        _invoiceRepository = invoiceRepository;
        _clinicRepository = clinicRepository;
        _patientRepository = patientRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<IReadOnlyList<InvoiceDto>>> Handle(GetInvoicesQuery request, CancellationToken cancellationToken)
    {
        var invoices = await _invoiceRepository.GetByTenantIdAsync(request.TenantId, request.ClinicId, cancellationToken);
        var clinics = await _clinicRepository.ListAsync(request.TenantId, cancellationToken);
        var clinicMap = clinics.ToDictionary(c => c.Id, c => c.NameEn);

        var dtos = new List<InvoiceDto>();

        foreach (var inv in invoices)
        {
            clinicMap.TryGetValue(inv.ClinicId, out var clinicName);
            string? patientName = null;
            var patient = await _patientRepository.GetByIdAsync(inv.PatientId, cancellationToken);
            if (patient is not null)
            {
                var user = await _userRepository.GetByIdAsync(patient.UserId, cancellationToken);
                patientName = user?.FullName;
            }

            dtos.Add(new InvoiceDto(
                inv.Id,
                inv.TenantId,
                inv.AppointmentId,
                inv.PatientId,
                inv.ClinicId,
                inv.InvoiceNumber,
                inv.SubTotal,
                inv.DiscountAmount,
                inv.TaxAmount,
                inv.TotalAmount,
                inv.PaidAmount,
                inv.Status,
                inv.PaymentMethod,
                inv.PaidAtUtc,
                inv.IssuedAtUtc,
                inv.Items.Select(i => new InvoiceItemDto(i.Id, i.Description, i.Quantity, i.UnitPrice, i.TotalPrice)).ToList(),
                patientName,
                clinicName));
        }

        return Result.Success<IReadOnlyList<InvoiceDto>>(dtos);
    }
}
