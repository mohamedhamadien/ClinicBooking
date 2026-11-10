using ClinicBooking.Domain.Common;

namespace ClinicBooking.Domain.Entities;

public sealed class InsuranceProvider : Entity<Guid>, IMayHaveTenant
{
    private InsuranceProvider() { }

    public string Name { get; private set; } = string.Empty;
    public string? NameAr { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public decimal DefaultCoveragePercentage { get; private set; } = 80.0m; // 80% coverage
    public string? ContactPhone { get; private set; }
    public bool IsActive { get; private set; } = true;
    public Guid? TenantId { get; private set; }

    public static InsuranceProvider Create(
        string name,
        string? nameAr,
        string code,
        decimal defaultCoveragePercentage,
        string? contactPhone,
        Guid? tenantId = null)
    {
        return new InsuranceProvider
        {
            Id = Guid.NewGuid(),
            Name = name,
            NameAr = nameAr,
            Code = code.ToUpperInvariant().Trim(),
            DefaultCoveragePercentage = Math.Clamp(defaultCoveragePercentage, 0m, 100m),
            ContactPhone = contactPhone,
            IsActive = true,
            TenantId = tenantId
        };
    }

    public void Update(string name, string? nameAr, string code, decimal defaultCoveragePercentage, string? contactPhone, bool isActive)
    {
        Name = name;
        NameAr = nameAr;
        Code = code.ToUpperInvariant().Trim();
        DefaultCoveragePercentage = Math.Clamp(defaultCoveragePercentage, 0m, 100m);
        ContactPhone = contactPhone;
        IsActive = isActive;
    }
}
