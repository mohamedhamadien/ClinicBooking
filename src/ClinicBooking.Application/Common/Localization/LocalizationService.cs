using System.Globalization;
using ClinicBooking.Application.Contracts.Services;
using ClinicBooking.Domain.Enums;

namespace ClinicBooking.Application.Common.Localization;

public sealed class LocalizationService : ILocalizationService
{
    private static readonly Dictionary<string, string> EnglishTranslations = new(StringComparer.OrdinalIgnoreCase)
    {
        // Appointments
        [LocalizationKeys.AppointmentDoctorUnavailable] = "The doctor is not available at the specified time.",
        [LocalizationKeys.AppointmentSlotAlreadyBooked] = "This appointment slot is already booked.",
        [LocalizationKeys.AppointmentPastAppointment] = "Cannot book an appointment in the past.",
        [LocalizationKeys.AppointmentPatientOverlapping] = "Patient already has an active overlapping appointment at this time.",
        [LocalizationKeys.AppointmentNotFound] = "Appointment was not found.",
        [LocalizationKeys.AppointmentCancellationNoticeExpired] = "Appointment cannot be cancelled as the required cancellation notice period has expired.",
        [LocalizationKeys.AppointmentInvalidStatusTransition] = "Invalid appointment status transition.",
        [LocalizationKeys.AppointmentBookingSuccess] = "Appointment booked successfully.",
        [LocalizationKeys.AppointmentCancellationSuccess] = "Appointment cancelled successfully.",

        // Entities
        [LocalizationKeys.DoctorNotFound] = "Doctor with ID '{0}' was not found.",
        [LocalizationKeys.PatientNotFound] = "Patient with ID '{0}' was not found.",
        [LocalizationKeys.ClinicNotFound] = "Clinic with ID '{0}' was not found.",
        [LocalizationKeys.SpecialtyNotFound] = "Specialty with ID '{0}' was not found.",
        [LocalizationKeys.TenantNotFound] = "Tenant with ID '{0}' was not found.",
        [LocalizationKeys.TenantDuplicateIdentifier] = "A tenant with identifier '{0}' already exists.",

        // Auth
        [LocalizationKeys.AuthInvalidCredentials] = "Invalid email or password.",
        [LocalizationKeys.AuthEmailAlreadyRegistered] = "Email '{0}' is already registered.",
        [LocalizationKeys.AuthInvalidToken] = "Invalid or expired token.",
        [LocalizationKeys.AuthUnauthorized] = "You are not authorized to perform this operation.",
        [LocalizationKeys.AuthForbidden] = "Access to this resource is forbidden.",

        // Validation
        [LocalizationKeys.ValidationRequired] = "The field '{0}' is required.",
        [LocalizationKeys.ValidationInvalidEmail] = "The field '{0}' is not a valid email address.",
        [LocalizationKeys.ValidationInvalidPhone] = "The field '{0}' is not a valid phone number format.",
        [LocalizationKeys.ValidationSlotDurationRange] = "Default slot duration must be between 10 and 180 minutes.",
        [LocalizationKeys.ValidationCancellationNoticeRange] = "Cancellation notice hours must be between 0 and 168 hours (7 days).",
        [LocalizationKeys.ValidationIdentifierFormat] = "Identifier must consist of lowercase alphanumeric characters and hyphens."
    };

    private static readonly Dictionary<string, string> ArabicTranslations = new(StringComparer.OrdinalIgnoreCase)
    {
        // Appointments
        [LocalizationKeys.AppointmentDoctorUnavailable] = "الطبيب غير متاح في هذا الوقت المحدد.",
        [LocalizationKeys.AppointmentSlotAlreadyBooked] = "هذا الموعد محجوز بالفعل لمريض آخر.",
        [LocalizationKeys.AppointmentPastAppointment] = "لا يمكن حجز موعد في تاريخ أو وقت سابق.",
        [LocalizationKeys.AppointmentPatientOverlapping] = "لديك موعد آخر متداخل في هذا الوقت.",
        [LocalizationKeys.AppointmentNotFound] = "لم يتم العثور على الموعد المطلوب.",
        [LocalizationKeys.AppointmentCancellationNoticeExpired] = "لا يمكن إلغاء الموعد لانتهاء المهلة المحددة مسبقاً لإلغاء المواعيد.",
        [LocalizationKeys.AppointmentInvalidStatusTransition] = "تغيير حالة الموعد غير مسموح به وفقاً لقواعد النظام.",
        [LocalizationKeys.AppointmentBookingSuccess] = "تم حجز الموعد بنجاح.",
        [LocalizationKeys.AppointmentCancellationSuccess] = "تم إلغاء الموعد بنجاح.",

        // Entities
        [LocalizationKeys.DoctorNotFound] = "لم يتم العثور على الطبيب بالمعرف '{0}'.",
        [LocalizationKeys.PatientNotFound] = "لم يتم العثور على المريض بالمعرف '{0}'.",
        [LocalizationKeys.ClinicNotFound] = "لم يتم العثور على العيادة بالمعرف '{0}'.",
        [LocalizationKeys.SpecialtyNotFound] = "لم يتم العثور على التخصص بالمعرف '{0}'.",
        [LocalizationKeys.TenantNotFound] = "لم يتم العثور على المؤسسة/العيادة بالمعرف '{0}'.",
        [LocalizationKeys.TenantDuplicateIdentifier] = "مؤسسة بالمعرف '{0}' موجودة بالفعل.",

        // Auth
        [LocalizationKeys.AuthInvalidCredentials] = "البريد الإلكتروني أو كلمة المرور غير صحيحة.",
        [LocalizationKeys.AuthEmailAlreadyRegistered] = "البريد الإلكتروني '{0}' مسجل بالفعل.",
        [LocalizationKeys.AuthInvalidToken] = "رمز الدخول أو التجديد غير صالح أو منتهي الصلاحية.",
        [LocalizationKeys.AuthUnauthorized] = "غير مصرح لك بتنفيذ هذه العملية.",
        [LocalizationKeys.AuthForbidden] = "الوصول إلى هذا المورد محظور.",

        // Validation
        [LocalizationKeys.ValidationRequired] = "حقل '{0}' مطلوب ولا يمكن تركه فارغاً.",
        [LocalizationKeys.ValidationInvalidEmail] = "حقل '{0}' لا يحتوي على صيغة بريد إلكتروني صالحة.",
        [LocalizationKeys.ValidationInvalidPhone] = "حقل '{0}' لا يحتوي على صيغة رقم هاتف صالحة.",
        [LocalizationKeys.ValidationSlotDurationRange] = "مدة الكشف يجب أن تتراوح بين 10 دقائق و 180 دقيقة.",
        [LocalizationKeys.ValidationCancellationNoticeRange] = "مهلة الإلغاء يجب أن تكون بين 0 و 168 ساعة (7 أيام).",
        [LocalizationKeys.ValidationIdentifierFormat] = "المعرف يجب أن يتكون من حروف إنجليزية صغيرة وأرقام وشرطات فقط."
    };

    public string CurrentCulture => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

    public bool IsArabic => CurrentCulture.Equals("ar", StringComparison.OrdinalIgnoreCase);

    public string GetString(string key)
    {
        var dict = IsArabic ? ArabicTranslations : EnglishTranslations;

        if (dict.TryGetValue(key, out var translation))
        {
            return translation;
        }

        // Fallback to English if missing in Arabic
        if (EnglishTranslations.TryGetValue(key, out var fallback))
        {
            return fallback;
        }

        return key;
    }

    public string GetString(string key, params object[] args)
    {
        var template = GetString(key);
        try
        {
            return string.Format(CultureInfo.CurrentCulture, template, args);
        }
        catch (FormatException)
        {
            return template;
        }
    }

    public string GetStatusName(AppointmentStatus status)
    {
        if (IsArabic)
        {
            return status switch
            {
                AppointmentStatus.Scheduled => "مجدول",
                AppointmentStatus.PendingApproval => "في انتظار موافقة الطبيب",
                AppointmentStatus.Confirmed => "مؤكد",
                AppointmentStatus.CancelledByPatient => "ملغي من قِبل المريض",
                AppointmentStatus.CancelledByDoctor => "ملغي من قِبل الطبيب",
                AppointmentStatus.CancelledByAdmin => "ملغي من قِبل الإدارة",
                AppointmentStatus.Completed => "مكتمل",
                AppointmentStatus.NoShow => "لم يحضر المريض",
                AppointmentStatus.Rejected => "مرفوض",
                _ => status.ToString()
            };
        }

        return status switch
        {
            AppointmentStatus.Scheduled => "Scheduled",
            AppointmentStatus.PendingApproval => "Pending Doctor Approval",
            AppointmentStatus.Confirmed => "Confirmed",
            AppointmentStatus.CancelledByPatient => "Cancelled by Patient",
            AppointmentStatus.CancelledByDoctor => "Cancelled by Doctor",
            AppointmentStatus.CancelledByAdmin => "Cancelled by Admin",
            AppointmentStatus.Completed => "Completed",
            AppointmentStatus.NoShow => "No Show",
            AppointmentStatus.Rejected => "Rejected",
            _ => status.ToString()
        };
    }

    public string GetGenderName(Gender gender)
    {
        if (IsArabic)
        {
            return gender switch
            {
                Gender.Male => "ذكر",
                Gender.Female => "أنثى",
                _ => "غير محدد"
            };
        }

        return gender switch
        {
            Gender.Male => "Male",
            Gender.Female => "Female",
            _ => "Unspecified"
        };
    }

    public string GetDayOfWeekName(DayOfWeek dayOfWeek)
    {
        if (IsArabic)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Sunday => "الأحد",
                DayOfWeek.Monday => "الإثنين",
                DayOfWeek.Tuesday => "الثلاثاء",
                DayOfWeek.Wednesday => "الأربعاء",
                DayOfWeek.Thursday => "الخميس",
                DayOfWeek.Friday => "الجمعة",
                DayOfWeek.Saturday => "السبت",
                _ => dayOfWeek.ToString()
            };
        }

        return dayOfWeek.ToString();
    }
}
