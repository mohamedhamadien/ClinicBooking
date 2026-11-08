namespace ClinicBooking.Domain.Common;

public interface IMustHaveTenant
{
    Guid TenantId { get; }
}
