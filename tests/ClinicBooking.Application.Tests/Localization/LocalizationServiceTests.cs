using System.Globalization;
using ClinicBooking.Application.Common.Localization;
using ClinicBooking.Domain.Enums;

namespace ClinicBooking.Application.Tests.Localization;

public class LocalizationServiceTests
{
    [Theory]
    [InlineData("ar-EG")]
    [InlineData("ar-SA")]
    [InlineData("ar")]
    public void GetString_WhenArabicCulture_ReturnsArabicText(string cultureName)
    {
        // Arrange
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo(cultureName);
            var service = new LocalizationService();

            // Act
            var message = service.GetString(LocalizationKeys.AppointmentSlotAlreadyBooked);

            // Assert
            Assert.True(service.IsArabic);
            Assert.Equal("هذا الموعد محجوز بالفعل لمريض آخر.", message);
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("en-GB")]
    [InlineData("en")]
    public void GetString_WhenEnglishCulture_ReturnsEnglishText(string cultureName)
    {
        // Arrange
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo(cultureName);
            var service = new LocalizationService();

            // Act
            var message = service.GetString(LocalizationKeys.AppointmentSlotAlreadyBooked);

            // Assert
            Assert.False(service.IsArabic);
            Assert.Equal("This appointment slot is already booked.", message);
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Fact]
    public void GetString_WithArguments_FormatsCorrectlyInArabic()
    {
        // Arrange
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("ar-EG");
            var service = new LocalizationService();

            // Act
            var message = service.GetString(LocalizationKeys.DoctorNotFound, "DOC-1234");

            // Assert
            Assert.Equal("لم يتم العثور على الطبيب بالمعرف 'DOC-1234'.", message);
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Fact]
    public void GetStatusName_InArabic_TranslatesAllStatuses()
    {
        // Arrange
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("ar-EG");
            var service = new LocalizationService();

            // Act & Assert
            Assert.Equal("مجدول", service.GetStatusName(AppointmentStatus.Scheduled));
            Assert.Equal("في انتظار موافقة الطبيب", service.GetStatusName(AppointmentStatus.PendingApproval));
            Assert.Equal("مؤكد", service.GetStatusName(AppointmentStatus.Confirmed));
            Assert.Equal("مكتمل", service.GetStatusName(AppointmentStatus.Completed));
            Assert.Equal("ملغي من قِبل المريض", service.GetStatusName(AppointmentStatus.CancelledByPatient));
            Assert.Equal("ملغي من قِبل الطبيب", service.GetStatusName(AppointmentStatus.CancelledByDoctor));
            Assert.Equal("ملغي من قِبل الإدارة", service.GetStatusName(AppointmentStatus.CancelledByAdmin));
            Assert.Equal("مرفوض", service.GetStatusName(AppointmentStatus.Rejected));
            Assert.Equal("لم يحضر المريض", service.GetStatusName(AppointmentStatus.NoShow));
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Fact]
    public void GetDayOfWeekName_InArabic_TranslatesDays()
    {
        // Arrange
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("ar-EG");
            var service = new LocalizationService();

            // Act & Assert
            Assert.Equal("الأحد", service.GetDayOfWeekName(DayOfWeek.Sunday));
            Assert.Equal("الإثنين", service.GetDayOfWeekName(DayOfWeek.Monday));
            Assert.Equal("الثلاثاء", service.GetDayOfWeekName(DayOfWeek.Tuesday));
            Assert.Equal("الأربعاء", service.GetDayOfWeekName(DayOfWeek.Wednesday));
            Assert.Equal("الخميس", service.GetDayOfWeekName(DayOfWeek.Thursday));
            Assert.Equal("الجمعة", service.GetDayOfWeekName(DayOfWeek.Friday));
            Assert.Equal("السبت", service.GetDayOfWeekName(DayOfWeek.Saturday));
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Fact]
    public void GetGenderName_InArabic_TranslatesGenders()
    {
        // Arrange
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("ar-EG");
            var service = new LocalizationService();

            // Act & Assert
            Assert.Equal("ذكر", service.GetGenderName(Gender.Male));
            Assert.Equal("أنثى", service.GetGenderName(Gender.Female));
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }
}
