using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Features.Insurance.Commands.CreateInsuranceProvider;
using ClinicBooking.Application.Features.Insurance.Queries.GetInsuranceProviders;
using ClinicBooking.Application.Features.Inventory.Commands.AdjustInventoryStock;
using ClinicBooking.Application.Features.Inventory.Commands.CreateInventoryItem;
using ClinicBooking.Application.Features.Inventory.Queries.GetClinicInventory;
using ClinicBooking.Application.Features.Invoices.Commands.PayInvoice;
using ClinicBooking.Application.Features.MedicalAttachments.Commands.UploadMedicalAttachment;
using ClinicBooking.Application.Features.MedicalAttachments.Queries.GetPatientAttachments;
using ClinicBooking.Application.Tests.TestHelpers;
using ClinicBooking.Domain.Common;
using ClinicBooking.Domain.Entities;
using ClinicBooking.Domain.Enums;
using Xunit;

namespace ClinicBooking.Application.Tests.Features;

public class NewFeaturesTests
{
    private readonly FakeMedicalAttachmentRepository _attachmentRepo = new();
    private readonly FakeMedicalInventoryRepository _inventoryRepo = new();
    private readonly FakeInsuranceProviderRepository _insuranceRepo = new();
    private readonly FakeClinicRepository _clinicRepo = new();
    private readonly FakeUnitOfWork _uow = new();

    [Fact]
    public async Task UploadMedicalAttachment_Should_Create_And_Return_AttachmentDto()
    {
        // Arrange
        var handler = new UploadMedicalAttachmentCommandHandler(_attachmentRepo, _uow);
        var patientId = Guid.NewGuid();
        var command = new UploadMedicalAttachmentCommand(
            patientId,
            "CBC Blood Panel",
            "LabResult",
            "cbc_result.pdf",
            "application/pdf",
            1024,
            "data:application/pdf;base64,AAAA",
            "Normal parameters");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("CBC Blood Panel", result.Value.Title);
        Assert.Equal("LabResult", result.Value.AttachmentType);
        Assert.Single(_attachmentRepo.Items);
    }

    [Fact]
    public async Task CreateInventoryItem_Should_Succeed_When_Clinic_Exists()
    {
        // Arrange
        var clinic = Clinic.Create(Guid.NewGuid(), "Main Clinic", "123 St", "Cairo", "+201000000000");
        await _clinicRepo.AddAsync(clinic);

        var handler = new CreateInventoryItemCommandHandler(_inventoryRepo, _clinicRepo, _uow);
        var command = new CreateInventoryItemCommand(
            clinic.Id,
            "Sterile Syringes 5ml",
            "سرنجات معقمة 5 مل",
            "Consumables",
            100,
            20,
            "Box",
            50m,
            "BATCH-001",
            DateTime.UtcNow.AddYears(1));

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Sterile Syringes 5ml", result.Value.ItemName);
        Assert.Equal(100, result.Value.CurrentStock);
        Assert.False(result.Value.IsLowStock);
        Assert.Single(_inventoryRepo.Items);
    }

    [Fact]
    public async Task AdjustInventoryStock_Should_Increase_Or_Decrease_Stock()
    {
        // Arrange
        var clinicId = Guid.NewGuid();
        var item = MedicalInventoryItem.Create(
            clinicId, "Test Gauze", "شاش", "Consumables", 50, 10, "Box", 20m, "BATCH-1", null);
        await _inventoryRepo.AddAsync(item);

        var handler = new AdjustInventoryStockCommandHandler(_inventoryRepo, _uow);

        // Act: add 15 items
        var result = await handler.Handle(new AdjustInventoryStockCommand(item.Id, 15), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(65, result.Value.CurrentStock);
    }

    [Fact]
    public async Task CreateInsuranceProvider_Should_Create_Provider_With_Coverage()
    {
        // Arrange
        var handler = new CreateInsuranceProviderCommandHandler(_insuranceRepo, _uow);
        var command = new CreateInsuranceProviderCommand(
            "Bupa Global",
            "بوبا العالمية",
            "BUPA-01",
            80m,
            "+20224001000");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("BUPA-01", result.Value.Code);
        Assert.Equal(80m, result.Value.DefaultCoveragePercentage);
        Assert.Single(_insuranceRepo.Items);
    }

    [Fact]
    public async Task PayInvoice_Should_Support_Partial_Payment_And_Set_Status_To_PartiallyPaid()
    {
        // Arrange
        var invoiceRepo = new FakeInvoiceRepository();
        var patientRepo = new FakePatientRepository();
        var userRepo = new FakeUserRepository();
        var handler = new PayInvoiceCommandHandler(invoiceRepo, _clinicRepo, patientRepo, userRepo, _uow);

        var tenantId = Guid.NewGuid();
        var clinicId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var invoice = Invoice.Create(tenantId, null, patientId, clinicId, 0, 0, PaymentMethod.Cash, "INV-TEST-001");
        invoice.AddItem("General Consultation", 1, 300m);
        invoice.AddItem("Blood Test", 1, 200m); // Total = 500m
        await invoiceRepo.AddAsync(invoice);

        // Act: Pay 200 out of 500
        var command = new PayInvoiceCommand(invoice.Id, PaymentMethod.CreditCard, 200m);
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(InvoiceStatus.PartiallyPaid, result.Value.Status);
        Assert.Equal(200m, result.Value.PaidAmount);
        Assert.Equal(300m, result.Value.RemainingAmount);
        Assert.Equal(500m, result.Value.TotalAmount);

        // Act 2: Pay remaining 300
        var command2 = new PayInvoiceCommand(invoice.Id, PaymentMethod.Cash, 300m);
        var result2 = await handler.Handle(command2, CancellationToken.None);

        // Assert 2
        Assert.True(result2.IsSuccess);
        Assert.Equal(InvoiceStatus.Paid, result2.Value.Status);
        Assert.Equal(500m, result2.Value.PaidAmount);
        Assert.Equal(0m, result2.Value.RemainingAmount);
    }

    [Fact]
    public void ConsultationNote_Should_Hold_FollowUpDate_And_LabRequests()
    {
        // Arrange
        var followUpDate = DateTime.UtcNow.AddDays(7);
        var labRequests = "CBC, Fasting Blood Sugar, Chest X-Ray";

        // Act
        var note = ConsultationNote.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Persistent cough",
            "Chest clear",
            "Acute Bronchitis",
            "Antibiotics and rest",
            DateTime.UtcNow,
            followUpDate,
            labRequests);

        // Assert
        Assert.Equal(labRequests, note.LabRequests);
        Assert.NotNull(note.FollowUpDateUtc);
        Assert.Equal(followUpDate.Date, note.FollowUpDateUtc.Value.Date);
    }

    [Fact]
    public void SoftDeletable_Entities_Should_Implement_ISoftDeletable()
    {
        // Assert
        Assert.True(typeof(ISoftDeletable).IsAssignableFrom(typeof(Patient)));
        Assert.True(typeof(ISoftDeletable).IsAssignableFrom(typeof(MedicalAttachment)));
    }
}

internal sealed class FakeInvoiceRepository : IInvoiceRepository
{
    public readonly List<Invoice> Items = [];

    public Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Items.FirstOrDefault(i => i.Id == id));
    }

    public Task<Invoice?> GetByAppointmentIdAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Items.FirstOrDefault(i => i.AppointmentId == appointmentId));
    }

    public Task<IReadOnlyList<Invoice>> GetByTenantIdAsync(Guid tenantId, Guid? clinicId = null, CancellationToken cancellationToken = default)
    {
        var q = Items.Where(i => i.TenantId == tenantId);
        if (clinicId.HasValue) q = q.Where(i => i.ClinicId == clinicId.Value);
        return Task.FromResult<IReadOnlyList<Invoice>>(q.ToList());
    }

    public Task<IReadOnlyList<Invoice>> GetByPatientIdAsync(Guid tenantId, Guid patientId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Invoice>>(Items.Where(i => i.TenantId == tenantId && i.PatientId == patientId).ToList());
    }

    public Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        Items.Add(invoice);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        var idx = Items.FindIndex(i => i.Id == invoice.Id);
        if (idx >= 0) Items[idx] = invoice;
        return Task.CompletedTask;
    }
}

internal sealed class FakeMedicalAttachmentRepository : IMedicalAttachmentRepository
{
    public readonly List<MedicalAttachment> Items = [];

    public Task<MedicalAttachment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Items.FirstOrDefault(i => i.Id == id));
    }

    public Task<IReadOnlyList<MedicalAttachment>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<MedicalAttachment>>(Items.Where(i => i.PatientId == patientId).ToList());
    }

    public Task AddAsync(MedicalAttachment attachment, CancellationToken cancellationToken = default)
    {
        Items.Add(attachment);
        return Task.CompletedTask;
    }

    public void Delete(MedicalAttachment attachment)
    {
        Items.Remove(attachment);
    }
}

internal sealed class FakeMedicalInventoryRepository : IMedicalInventoryRepository
{
    public readonly List<MedicalInventoryItem> Items = [];

    public Task<MedicalInventoryItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Items.FirstOrDefault(i => i.Id == id));
    }

    public Task<IReadOnlyList<MedicalInventoryItem>> GetByClinicIdAsync(Guid clinicId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<MedicalInventoryItem>>(Items.Where(i => i.ClinicId == clinicId).ToList());
    }

    public Task<IReadOnlyList<MedicalInventoryItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<MedicalInventoryItem>>(Items.ToList());
    }

    public Task AddAsync(MedicalInventoryItem item, CancellationToken cancellationToken = default)
    {
        Items.Add(item);
        return Task.CompletedTask;
    }

    public void Delete(MedicalInventoryItem item)
    {
        Items.Remove(item);
    }
}

internal sealed class FakeInsuranceProviderRepository : IInsuranceProviderRepository
{
    public readonly List<InsuranceProvider> Items = [];

    public Task<InsuranceProvider?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Items.FirstOrDefault(i => i.Id == id));
    }

    public Task<IReadOnlyList<InsuranceProvider>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<InsuranceProvider>>(Items.ToList());
    }

    public Task AddAsync(InsuranceProvider provider, CancellationToken cancellationToken = default)
    {
        Items.Add(provider);
        return Task.CompletedTask;
    }
}
