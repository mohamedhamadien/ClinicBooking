using ClinicBooking.Application.Contracts.Services;

namespace ClinicBooking.Infrastructure.Services;

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
