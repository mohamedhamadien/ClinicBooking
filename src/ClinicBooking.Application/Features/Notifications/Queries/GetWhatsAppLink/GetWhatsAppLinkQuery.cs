using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Features.Notifications.DTOs;
using MediatR;

namespace ClinicBooking.Application.Features.Notifications.Queries.GetWhatsAppLink;

public sealed record GetWhatsAppLinkQuery(Guid AppointmentId) : IRequest<Result<WhatsAppReminderLinkDto>>;
