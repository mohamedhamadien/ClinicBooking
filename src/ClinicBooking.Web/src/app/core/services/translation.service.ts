import { Injectable, signal, effect } from '@angular/core';

export type Language = 'en' | 'ar';

export interface Translations {
  [key: string]: {
    en: string;
    ar: string;
  };
}

const TRANSLATIONS: Translations = {
  // Navigation
  'nav.brand': { en: 'Apex Health Clinic', ar: 'عيادات أبيكس الطبية' },
  'nav.home': { en: 'Home', ar: 'الرئيسية' },
  'nav.doctors': { en: 'Find Doctors', ar: 'الأطباء' },
  'nav.my_appointments': { en: 'My Appointments', ar: 'مواعيدي' },
  'nav.dashboard': { en: 'Admin Dashboard', ar: 'لوحة التحكم' },
  'nav.specialties': { en: 'Specialties', ar: 'التخصصات' },
  'nav.login': { en: 'Sign In', ar: 'تسجيل الدخول' },
  'nav.register': { en: 'Register', ar: 'إنشاء حساب' },
  'nav.logout': { en: 'Sign Out', ar: 'تسجيل الخروج' },

  // Home Hero
  'home.hero.title': { en: 'Excellence in Healthcare, Just a Click Away', ar: 'رعاية صحية متميزة، على بُعد نقرة واحدة' },
  'home.hero.subtitle': {
    en: 'Book verified specialist doctors with guaranteed real-time slot availability and zero double-booking.',
    ar: 'احجز أفضل الأطباء والاستشاريين مع تأكيد فوري للمواعيد بدون أي تعارض أو حجز مزدوج.'
  },
  'home.hero.cta_book': { en: 'Book an Appointment', ar: 'احجز موعدك الآن' },
  'home.hero.cta_doctors': { en: 'Browse Specialists', ar: 'تصفح التخصصات' },

  // Features
  'home.features.title': { en: 'Why Choose Apex Health?', ar: 'لماذا تختار أبيكس هيلث؟' },
  'home.features.f1_title': { en: 'Zero Double-Booking', ar: 'منع الحجز المزدوج نهائياً' },
  'home.features.f1_desc': {
    en: 'Real-time database concurrency locks ensure your selected time slot is exclusively yours.',
    ar: 'تقنية قفل المعاملات الفورية تضمن أن الموعد المختار محجوز لك وحدك بدون أي خطأ.'
  },
  'home.features.f2_title': { en: 'Bilingual Care', ar: 'دعم كامل للغتين' },
  'home.features.f2_desc': {
    en: 'Complete English and Arabic interface with native RTL and bilingual prescriptions.',
    ar: 'واجهة متكاملة باللغتين العربية والإنجليزية مع دعم كامل للاتجاه من اليمين لليسار.'
  },
  'home.features.f3_title': { en: 'Multi-Clinic Network', ar: 'شبكة عيادات متعددة' },
  'home.features.f3_desc': {
    en: 'Visit doctors across multiple modern branches with unified medical records.',
    ar: 'زيارة نخبة الأطباء عبر فروع متعددة مع ملف طبي وسجل مواعيد موحد.'
  },

  // Auth
  'auth.login.title': { en: 'Welcome Back', ar: 'مرحباً بك مجدداً' },
  'auth.login.subtitle': { en: 'Sign in to manage your appointments and records', ar: 'سجل الدخول لإدارة مواعيدك وسجلاتك الطبية' },
  'auth.login.email': { en: 'Email Address', ar: 'البريد الإلكتروني' },
  'auth.login.password': { en: 'Password', ar: 'كلمة المرور' },
  'auth.login.submit': { en: 'Sign In', ar: 'تسجيل الدخول' },
  'auth.login.no_account': { en: "Don't have an account?", ar: 'ليس لديك حساب؟' },
  'auth.login.register_here': { en: 'Register as a patient', ar: 'سجل حساب مريض جديد' },

  'auth.register.title': { en: 'Patient Registration', ar: 'تسجيل مريض جديد' },
  'auth.register.subtitle': { en: 'Create your patient account to book appointments', ar: 'أنشئ حسابك الطبي لتتمكن من حجز المواعيد' },
  'auth.register.first_name': { en: 'First Name', ar: 'الاسم الأول' },
  'auth.register.last_name': { en: 'Last Name', ar: 'اسم العائلة' },
  'auth.register.dob': { en: 'Date of Birth', ar: 'تاريخ الميلاد' },
  'auth.register.gender': { en: 'Gender', ar: 'النوع' },
  'auth.register.gender_male': { en: 'Male', ar: 'ذكر' },
  'auth.register.gender_female': { en: 'Female', ar: 'أنثى' },
  'auth.register.gender_other': { en: 'Other', ar: 'آخر' },
  'auth.register.emergency': { en: 'Emergency Contact Phone', ar: 'رقم هاتف الطوارئ' },
  'auth.register.submit': { en: 'Create Account', ar: 'إنشاء الحساب' },
  'auth.register.has_account': { en: 'Already have an account?', ar: 'هل لديك حساب بالفعل؟' },

  // Doctors
  'doctors.title': { en: 'Our Medical Specialists', ar: 'أطباؤنا والاستشاريون' },
  'doctors.subtitle': { en: 'Find trusted doctors across multiple clinical specialties', ar: 'ابحث عن نخبة الأطباء عبر مختلف التخصصات الطبية' },
  'doctors.filter_specialty': { en: 'All Specialties', ar: 'جميع التخصصات' },
  'doctors.filter_clinic': { en: 'All Clinics', ar: 'جميع العيادات' },
  'doctors.search_placeholder': { en: 'Search by doctor name or license...', ar: 'ابحث باسم الطبيب أو رقم الترخيص...' },
  'doctors.book_btn': { en: 'Book Appointment', ar: 'احجز موعد' },
  'doctors.fee': { en: 'Consultation Fee', ar: 'قيمة الكشف' },
  'doctors.slot_duration': { en: 'Slot Duration', ar: 'مدة الكشف' },
  'doctors.minutes': { en: 'mins', ar: 'دقيقة' },
  'doctors.no_doctors': { en: 'No doctors found matching your criteria.', ar: 'لم يتم العثور على أطباء يطابقون بحثك.' },

  // Booking
  'booking.title': { en: 'Schedule Appointment', ar: 'تحديد موعد الكشف' },
  'booking.select_clinic': { en: 'Select Clinic Branch', ar: 'اختر فرع العيادة' },
  'booking.select_date': { en: 'Select Date', ar: 'اختر التاريخ' },
  'booking.available_slots': { en: 'Available Slots', ar: 'المواعيد المتاحة' },
  'booking.no_slots': { en: 'No slots available on this date. Please pick another date.', ar: 'لا توجد مواعيد متاحة في هذا اليوم، يرجى اختيار تاريخ آخر.' },
  'booking.notes': { en: 'Notes / Symptoms (Optional)', ar: 'ملاحظات أو أعراض (اختياري)' },
  'booking.confirm_btn': { en: 'Confirm Booking', ar: 'تأكيد الحجز' },
  'booking.success': { en: 'Appointment booked successfully!', ar: 'تم تأكيد حجز الموعد بنجاح!' },

  // My Appointments
  'appointments.title': { en: 'My Appointments', ar: 'سجل مواعيدي' },
  'appointments.subtitle': { en: 'Review and manage your scheduled and past appointments', ar: 'متابعة وإدارة المواعيد القادمة والسابقة' },
  'appointments.doctor': { en: 'Doctor', ar: 'الطبيب' },
  'appointments.clinic': { en: 'Clinic', ar: 'العيادة' },
  'appointments.datetime': { en: 'Date & Time', ar: 'الموعد' },
  'appointments.status': { en: 'Status', ar: 'الحالة' },
  'appointments.actions': { en: 'Actions', ar: 'إجراءات' },
  'appointments.cancel_btn': { en: 'Cancel', ar: 'إلغاء' },
  'appointments.reschedule_btn': { en: 'Reschedule', ar: 'تعديل الموعد' },
  'appointments.no_appointments': { en: 'You have no appointments yet.', ar: 'لا توجد لديك مواعيد مسجلة حتى الآن.' },

  // Statuses
  'status.Scheduled': { en: 'Scheduled', ar: 'مجدول' },
  'status.Confirmed': { en: 'Confirmed', ar: 'مؤكد' },
  'status.Completed': { en: 'Completed', ar: 'مكتمل' },
  'status.CancelledByPatient': { en: 'Cancelled by Patient', ar: 'ملغي بواسطة المريض' },
  'status.CancelledByDoctor': { en: 'Cancelled by Doctor', ar: 'ملغي بواسطة الطبيب' },
  'status.CancelledByAdmin': { en: 'Cancelled by Admin', ar: 'ملغي بواسطة الإدارة' },
  'status.Rejected': { en: 'Rejected', ar: 'مرفوض' },
  'status.NoShow': { en: 'No Show', ar: 'لم يحضر' },

  // Admin Dashboard
  'admin.dashboard.title': { en: 'Clinic Administration Dashboard', ar: 'لوحة قيادة إدارة العيادات' },
  'admin.dashboard.stats_total': { en: 'Total Bookings', ar: 'إجمالي الحجوزات' },
  'admin.dashboard.stats_scheduled': { en: 'Scheduled', ar: 'المجدولة' },
  'admin.dashboard.stats_confirmed': { en: 'Confirmed', ar: 'المؤكدة' },
  'admin.dashboard.stats_completed': { en: 'Completed', ar: 'المكتملة' },
  'admin.dashboard.stats_cancelled': { en: 'Cancelled', ar: 'الملغاة' },
  'admin.dashboard.stats_patients': { en: 'Unique Patients', ar: 'إجمالي المرضى' },
  'admin.dashboard.recent_bookings': { en: 'Recent Appointments across All Clinics', ar: 'أحدث الحجوزات عبر كافة الفروع' },
};

@Injectable({
  providedIn: 'root'
})
export class TranslationService {
  private readonly STORAGE_KEY = 'clinic_lang';
  
  // Current language signal ('en' or 'ar')
  public readonly currentLang = signal<Language>(this.getInitialLanguage());

  constructor() {
    // Reactive effect: automatically update document dir and lang attribute
    effect(() => {
      const lang = this.currentLang();
      localStorage.setItem(this.STORAGE_KEY, lang);
      document.documentElement.lang = lang;
      document.documentElement.dir = lang === 'ar' ? 'rtl' : 'ltr';
    });
  }

  public setLanguage(lang: Language): void {
    this.currentLang.set(lang);
  }

  public toggleLanguage(): void {
    this.setLanguage(this.currentLang() === 'en' ? 'ar' : 'en');
  }

  public isArabic(): boolean {
    return this.currentLang() === 'ar';
  }

  public translate(key: string): string {
    const entry = TRANSLATIONS[key];
    if (!entry) return key;
    return entry[this.currentLang()] || entry.en || key;
  }

  private getInitialLanguage(): Language {
    const saved = localStorage.getItem(this.STORAGE_KEY);
    if (saved === 'ar' || saved === 'en') return saved;
    return 'en';
  }
}
