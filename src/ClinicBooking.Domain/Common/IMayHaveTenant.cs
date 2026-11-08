namespace ClinicBooking.Domain.Common;

public interface IMayHaveTenant
{
    Guid? TenantId { get; }
}
