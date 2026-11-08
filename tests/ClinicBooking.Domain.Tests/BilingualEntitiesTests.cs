using ClinicBooking.Domain.Entities;
using ClinicBooking.Domain.Exceptions;

namespace ClinicBooking.Domain.Tests;

public class BilingualEntitiesTests
{
    [Fact]
    public void Specialty_WithArabicAndEnglish_StoresBothAndReturnsLocalized()
    {
        // Arrange & Act
        var specialty = Specialty.Create(
            nameEn: "Cardiology",
            descriptionEn: "Heart and cardiovascular system care",
            nameAr: "أمراض القلب والأوعية الدموية",
            descriptionAr: "رعاية القلب والأوعية الدموية");

        // Assert
        Assert.Equal("Cardiology", specialty.NameEn);
        Assert.Equal("أمراض القلب والأوعية الدموية", specialty.NameAr);
        Assert.Equal("Cardiology", specialty.Name);
        Assert.Equal("أمراض القلب والأوعية الدموية", specialty.GetLocalizedName(isArabic: true));
        Assert.Equal("Cardiology", specialty.GetLocalizedName(isArabic: false));
        Assert.Equal("رعاية القلب والأوعية الدموية", specialty.GetLocalizedDescription(isArabic: true));
        Assert.Equal("Heart and cardiovascular system care", specialty.GetLocalizedDescription(isArabic: false));
    }

    [Fact]
    public void Specialty_WhenArabicOmitted_FallsBackToEnglish()
    {
        // Arrange & Act
        var specialty = Specialty.Create(
            nameEn: "Neurology",
            descriptionEn: "Brain and nervous system disorders");

        // Assert
        Assert.Equal("Neurology", specialty.NameEn);
        Assert.Equal("Neurology", specialty.NameAr);
        Assert.Equal("Neurology", specialty.GetLocalizedName(isArabic: true));
    }

    [Fact]
    public void Clinic_WithArabicAndEnglish_StoresBothAndReturnsLocalized()
    {
        // Arrange & Act
        var clinic = Clinic.Create(
            tenantId: Guid.NewGuid(),
            nameEn: "Downtown Branch",
            addressEn: "123 Nile Corniche",
            cityEn: "Cairo",
            phoneNumber: "+201012345678",
            nameAr: "فرع وسط البلد",
            addressAr: "١٢٣ كورنيش النيل",
            cityAr: "القاهرة");

        // Assert
        Assert.Equal("Downtown Branch", clinic.NameEn);
        Assert.Equal("فرع وسط البلد", clinic.NameAr);
        Assert.Equal("Cairo", clinic.CityEn);
        Assert.Equal("القاهرة", clinic.CityAr);
        Assert.Equal("فرع وسط البلد", clinic.GetLocalizedName(isArabic: true));
        Assert.Equal("Downtown Branch", clinic.GetLocalizedName(isArabic: false));
        Assert.Equal("القاهرة", clinic.GetLocalizedCity(isArabic: true));
        Assert.Equal("١٢٣ كورنيش النيل", clinic.GetLocalizedAddress(isArabic: true));
    }

    [Fact]
    public void Tenant_WithArabicAndEnglish_StoresBothAndReturnsLocalized()
    {
        // Arrange & Act
        var tenant = Tenant.Create(
            nameEn: "Al-Amal Medical Group",
            identifier: "al-amal-group",
            nameAr: "مجموعة الأمل الطبية");

        // Assert
        Assert.Equal("Al-Amal Medical Group", tenant.NameEn);
        Assert.Equal("مجموعة الأمل الطبية", tenant.NameAr);
        Assert.Equal("مجموعة الأمل الطبية", tenant.GetLocalizedName(isArabic: true));
        Assert.Equal("Al-Amal Medical Group", tenant.GetLocalizedName(isArabic: false));
    }

    [Fact]
    public void Doctor_WithArabicAndEnglishBio_StoresBothAndReturnsLocalized()
    {
        // Arrange & Act
        var doctor = Doctor.Create(
            tenantId: Guid.NewGuid(),
            userId: Guid.NewGuid(),
            specialtyId: Guid.NewGuid(),
            licenseNumber: "MED-123456",
            bioEn: "Consultant of Interventional Cardiology with 15 years experience.",
            bioAr: "استشاري أمراض القلب التداخلية بخبرة تزيد عن 15 عاماً.");

        // Assert
        Assert.Equal("Consultant of Interventional Cardiology with 15 years experience.", doctor.BioEn);
        Assert.Equal("استشاري أمراض القلب التداخلية بخبرة تزيد عن 15 عاماً.", doctor.BioAr);
        Assert.Equal("استشاري أمراض القلب التداخلية بخبرة تزيد عن 15 عاماً.", doctor.GetLocalizedBio(isArabic: true));
        Assert.Equal("Consultant of Interventional Cardiology with 15 years experience.", doctor.GetLocalizedBio(isArabic: false));
    }
}
