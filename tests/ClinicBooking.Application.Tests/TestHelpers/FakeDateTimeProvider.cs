using ClinicBooking.Application.Contracts.Services;

namespace ClinicBooking.Application.Tests.TestHelpers;

public class FakeDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow { get; set; } = new(2026, 11, 4, 10, 0, 0, DateTimeKind.Utc);
}
