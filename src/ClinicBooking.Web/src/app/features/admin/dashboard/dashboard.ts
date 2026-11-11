import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { DatePipe, CurrencyPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AdminService, AuditLogDto, RegisterDoctorRequest, UpdateClinicRequest } from '../../../core/services/admin.service';
import { DoctorService } from '../../../core/services/doctor.service';
import { ClinicalService } from '../../../core/services/clinical.service';
import { StaffService } from '../../../core/services/staff.service';
import { AttachmentsService } from '../../../core/services/attachments.service';
import { InventoryService } from '../../../core/services/inventory.service';
import { InsuranceService } from '../../../core/services/insurance.service';
import { AppointmentService } from '../../../core/services/appointment.service';
import { AuthService } from '../../../core/services/auth.service';
import { TranslationService } from '../../../core/services/translation.service';
import { ReportingService } from '../../../core/services/reporting.service';
import { TranslatePipe } from '../../../core/pipes/translate.pipe';
import { DashboardStatsDto } from '../../../core/models/dashboard.models';
import { AppointmentDto, AppointmentStatus } from '../../../core/models/appointment.models';
import {
  AvailableSlotDto,
  ClinicDto,
  DoctorDto,
  SpecialtyDto,
  SetDoctorAvailabilityRequest,
  AddAvailabilityExceptionRequest
} from '../../../core/models/doctor.models';
import {
  ConsultationDto,
  InvoiceDto,
  PatientMedicalRecordDto,
  PatientSummaryDto,
  CreatePatientRequest,
  PrescriptionDto,
  QueueItemDto
} from '../../../core/models/clinical.models';
import {
  CreateStaffRequest,
  PermissionCatalogDto,
  PermissionItemDto,
  StaffMemberDto,
  UpdateStaffPermissionsRequest
} from '../../../core/models/staff.models';
import { MedicalAttachmentDto } from '../../../core/models/attachments.models';
import { MedicalInventoryItemDto } from '../../../core/models/inventory.models';
import { InsuranceProviderDto } from '../../../core/models/insurance.models';
import { NotificationService } from '../../../core/services/notification.service';
import { NotificationLogDto } from '../../../core/models/notification.models';

export interface ClinicServiceItem {
  id: string;
  code: string;
  nameEn: string;
  nameAr: string;
  category: 'Consultation' | 'Diagnostic' | 'Laboratory' | 'Procedure' | 'Dental' | 'Nursing';
  price: number;
}

export type AdminTab =
  | 'overview'
  | 'queue'
  | 'consultation'
  | 'records'
  | 'billing'
  | 'appointments'
  | 'doctors'
  | 'schedules'
  | 'clinics'
  | 'specialties'
  | 'staff'
  | 'inventory'
  | 'reports'
  | 'audit'
  | 'notifications';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [DatePipe, CurrencyPipe, FormsModule, TranslatePipe, RouterLink],
  templateUrl: './dashboard.html',
  styleUrls: ['./dashboard.scss']
})
export class DashboardComponent implements OnInit {
  private readonly adminService = inject(AdminService);
  private readonly doctorService = inject(DoctorService);
  private readonly clinicalService = inject(ClinicalService);
  private readonly staffService = inject(StaffService);
  private readonly attachmentsService = inject(AttachmentsService);
  private readonly inventoryService = inject(InventoryService);
  private readonly insuranceService = inject(InsuranceService);
  private readonly appointmentService = inject(AppointmentService);
  private readonly notificationService = inject(NotificationService);
  protected readonly auth = inject(AuthService);
  protected readonly translation = inject(TranslationService);
  protected readonly reporting = inject(ReportingService);

  // Active Tab & Sidebar State
  protected activeTab = signal<AdminTab>('overview');
  protected isSidebarCollapsed = signal(false);

  protected toggleSidebar(): void {
    this.isSidebarCollapsed.update((v) => !v);
  }

  protected getTabTitle(tab: AdminTab): string {
    const isAr = this.translation.isArabic();
    switch (tab) {
      case 'overview':
        return isAr ? '📊 المتابعة العامة ومؤشرات الأداء' : '📊 System Overview & Operational Metrics';
      case 'queue':
        return isAr ? '🛎️ طابور الانتظار وشاشة الاستقبال' : '🛎️ Reception & Patient Waiting Queue';
      case 'consultation':
        return isAr ? '📋 الكشف الطبي والفحص السريري والروشتة' : '📋 Clinical Examination (SOAP) & Prescriptions';
      case 'records':
        return isAr ? '🗂️ السجلات والملفات الطبية للمرضى (EMR)' : '🗂️ Patient Medical History & EMR Charts';
      case 'billing':
        return isAr ? '💳 فواتير العيادة والتحصيل المالي' : '💳 Clinic Invoices, Billing & Payments';
      case 'appointments':
        return isAr ? '📅 إدارة وجدول المواعيد والحجوزات' : '📅 Patient Appointments Management';
      case 'doctors':
        return isAr ? '👨‍⚕️ دليل الأطباء والكوادر الطبية' : '👨‍⚕️ Doctors & Medical Staff Directory';
      case 'schedules':
        return isAr ? '⏱️ جداول ومواعيد عمل الأطباء والعطلات الرسمية' : '⏱️ Doctor Working Schedules, Shifts & Holidays';
      case 'clinics':
        return isAr ? '🏥 الفروع والمراكز والعيادات' : '🏥 Clinic Facilities & Branch Management';
      case 'specialties':
        return isAr ? '🩺 التخصصات والأقسام الطبية' : '🩺 Medical Specialties & Departments';
      case 'staff':
        return isAr ? '👥 إدارة الموظفين والصلاحيات التفصيلية' : '👥 Staff Accounts & Granular Permissions';
      case 'inventory':
        return isAr ? '📦 المستلزمات والمخزون الطبي' : '📦 Medical Supplies & Inventory Management';
      default:
        return isAr ? 'لوحة التحكم' : 'Management Console';
    }
  }

  protected getActiveClinicName(): string {
    const ac = this.assignedClinic();
    if (ac) return this.translation.isArabic() && ac.nameAr ? ac.nameAr : ac.name;
    if (this.clinics().length > 0) {
      const c = this.clinics()[0];
      return this.translation.isArabic() && c.nameAr ? c.nameAr : c.name;
    }
    return this.translation.isArabic() ? 'الفرع الرئيسي' : 'Central Healthcare Clinic';
  }

  // Loaded Data Signals
  protected readonly stats = signal<DashboardStatsDto | null>(null);
  protected readonly appointments = signal<AppointmentDto[]>([]);
  protected readonly clinics = signal<ClinicDto[]>([]);
  protected readonly specialties = signal<SpecialtyDto[]>([]);
  protected readonly doctors = signal<DoctorDto[]>([]);

  // Clinical & Billing Data Signals
  protected readonly queueItems = signal<QueueItemDto[]>([]);
  protected readonly invoices = signal<InvoiceDto[]>([]);
  protected readonly patientRecord = signal<PatientMedicalRecordDto | null>(null);

  // Feature: Patients Directory & Registration
  protected readonly patients = signal<PatientSummaryDto[]>([]);
  protected readonly isAddPatientModalOpen = signal(false);
  protected readonly isSubmittingPatient = signal(false);
  protected patientSearchTerm = '';
  protected patFirstName = '';
  protected patLastName = '';
  protected patEmail = '';
  protected patPhone = '';
  protected patDob = '';
  protected patGender = 'Male';
  protected patBloodGroup = '';
  protected patAllergies = '';
  protected patChronicConditions = '';
  protected patEmergencyContact = '';
  protected patPassword = '';

  // Feature: Doctor Schedules, Shifts & Holidays State
  protected readonly selectedScheduleDoctorId = signal<string>('');
  protected readonly selectedScheduleDoctor = computed(() => {
    return this.doctors().find((d) => d.id === this.selectedScheduleDoctorId()) || null;
  });

  protected readonly weekDays = [
    { value: 0, nameEn: 'Sunday', nameAr: 'الأحد', isWeekend: false },
    { value: 1, nameEn: 'Monday', nameAr: 'الإثنين', isWeekend: false },
    { value: 2, nameEn: 'Tuesday', nameAr: 'الثلاثاء', isWeekend: false },
    { value: 3, nameEn: 'Wednesday', nameAr: 'الأربعاء', isWeekend: false },
    { value: 4, nameEn: 'Thursday', nameAr: 'الخميس', isWeekend: false },
    { value: 5, nameEn: 'Friday', nameAr: 'الجمعة (عطلة نهاية الأسبوع)', isWeekend: true },
    { value: 6, nameEn: 'Saturday', nameAr: 'السبت (عطلة نهاية الأسبوع)', isWeekend: true },
  ];

  protected scheduleClinicId = '';
  protected scheduleDayOfWeek = 0;
  protected scheduleStartTime = '09:00';
  protected scheduleEndTime = '15:00';
  protected readonly isSavingSchedule = signal(false);

  // Holiday / Blackout Exception Form
  protected holidayStartDate = '';
  protected holidayEndDate = '';
  protected holidayReason = 'إجازة رسمية / Official Holiday';
  protected readonly isSavingHoliday = signal(false);

  // Available Slots Live Calculator
  protected previewSlotDate = '';
  protected previewClinicId = '';
  protected readonly previewSlots = signal<AvailableSlotDto[]>([]);
  protected readonly isCheckingSlots = signal(false);
  protected readonly previewSlotStatusMessage = signal<string | null>(null);

  protected readonly isScheduleModalOpen = signal(false);

  // Staff / Reception Booking State
  protected readonly isBookAppointmentModalOpen = signal(false);
  protected readonly isBookingAppointment = signal(false);
  protected bookPatientId = '';
  protected bookDoctorId = '';
  protected bookClinicId = '';
  protected bookDate = '';
  protected bookSelectedSlot: AvailableSlotDto | null = null;
  protected bookNotes = '';
  protected bookImmediateCheckIn = false;
  protected readonly bookSlots = signal<AvailableSlotDto[]>([]);
  protected readonly isLoadingBookSlots = signal(false);
  protected readonly bookSlotsError = signal<string | null>(null);

  // Feature 2: Patient Attachments State
  protected readonly patientAttachments = signal<MedicalAttachmentDto[]>([]);
  protected isAttachmentModalOpen = false;
  protected previewAttachmentModal: MedicalAttachmentDto | null = null;
  protected attachTitle = '';
  protected attachType = 'LabResult';
  protected attachNotes = '';
  protected attachDataUrl = '';
  protected attachFileName = '';
  protected attachContentType = '';
  protected attachFileSizeBytes = 0;

  // Feature 4: Inventory State
  protected readonly inventoryItems = signal<MedicalInventoryItemDto[]>([]);
  protected filterInventorySearch = '';
  protected filterInventoryCategory = '';
  protected isAddInventoryModalOpen = false;
  protected invItemName = '';
  protected invItemNameAr = '';
  protected invCategory = 'Consumables';
  protected invStock = 50;
  protected invMinThreshold = 15;
  protected invUnit = 'Piece';
  protected invUnitPrice = 30;
  protected invBatch = '';
  protected invExpiry = '';

  protected readonly lowStockCount = computed(() =>
    this.inventoryItems().filter((i) => i.isLowStock).length
  );

  protected readonly filteredInventory = computed(() => {
    let list = this.inventoryItems();
    if (this.filterInventoryCategory) {
      list = list.filter((i) => i.category.toLowerCase() === this.filterInventoryCategory.toLowerCase());
    }
    if (this.filterInventorySearch.trim()) {
      const q = this.filterInventorySearch.trim().toLowerCase();
      list = list.filter((i) =>
        i.itemName.toLowerCase().includes(q) ||
        (i.itemNameAr && i.itemNameAr.toLowerCase().includes(q)) ||
        (i.batchNumber && i.batchNumber.toLowerCase().includes(q))
      );
    }
    return list;
  });

  // Feature 5: Visual Calendar View State
  protected readonly appointmentsViewMode = signal<'table' | 'calendar'>('table');
  protected readonly calendarHours = [8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20];
  protected quickViewAppointment: AppointmentDto | null = null;

  // Feature 6: Insurance State
  protected readonly insuranceProviders = signal<InsuranceProviderDto[]>([]);
  protected invInsuranceProviderId = '';
  protected invInsuranceCoverageAmount = 0;
  protected invPatientCopayAmount = 0;
  protected isInsuranceProviderModalOpen = false;
  protected newInsName = '';
  protected newInsNameAr = '';
  protected newInsCode = '';
  protected newInsCoverage = 80;
  protected newInsPhone = '';

  // Staff & Permissions State
  protected readonly staffMembers = signal<StaffMemberDto[]>([]);
  protected readonly permissionCatalog = signal<PermissionCatalogDto | null>(null);
  protected readonly showCreateStaffModal = signal(false);
  protected readonly showEditStaffModal = signal(false);

  protected filterStaffSearch = '';
  protected filterStaffRole = '';

  protected readonly screenPermissions = computed(() =>
    this.permissionCatalog()?.permissions.filter((p) => p.category === 'Screens') || []
  );

  protected readonly functionPermissions = computed(() =>
    this.permissionCatalog()?.permissions.filter((p) => p.category === 'Functions') || []
  );

  protected readonly filteredStaff = computed(() => {
    let list = this.staffMembers();
    if (this.filterStaffRole) {
      list = list.filter((s) => s.role.toLowerCase() === this.filterStaffRole.toLowerCase());
    }
    if (this.filterStaffSearch.trim()) {
      const q = this.filterStaffSearch.trim().toLowerCase();
      list = list.filter(
        (s) =>
          s.fullName.toLowerCase().includes(q) ||
          s.email.toLowerCase().includes(q) ||
          (s.clinicName && s.clinicName.toLowerCase().includes(q))
      );
    }
    return list;
  });

  // Tab Permission Requirements
  protected readonly tabPermissions: Record<AdminTab, string> = {
    overview: 'Screen.Overview',
    queue: 'Screen.Queue',
    consultation: 'Screen.Consultation',
    records: 'Screen.MedicalRecords',
    billing: 'Screen.Billing',
    appointments: 'Screen.Appointments',
    doctors: 'Screen.Doctors',
    schedules: 'Screen.Doctors',
    clinics: 'Screen.Clinics',
    specialties: 'Screen.Specialties',
    staff: 'Screen.Staff',
    inventory: 'Screen.Inventory',
    reports: 'Screen.Reports',
    audit: 'Screen.Audit',
    notifications: 'Screen.Notifications'
  };

  protected canAccessTab(tab: AdminTab): boolean {
    if (this.auth.isAdmin() || this.auth.isClinicAdmin()) return true;
    if (tab === 'reports' || tab === 'overview') {
      if (this.auth.isDoctor() || this.auth.isReceptionist()) return true;
    }
    if (tab === 'schedules' || tab === 'doctors') {
      if (this.auth.isDoctor()) return true;
    }
    return this.auth.canAccessScreen(this.tabPermissions[tab]);
  }

  protected readonly loading = signal(true);
  protected readonly actionMessage = signal<string | null>(null);
  protected readonly errorMessage = signal<string | null>(null);

  // Current Clinic Scope (if Clinic Admin)
  protected readonly assignedClinicId = computed(() => this.auth.currentUser()?.clinicId || null);
  protected readonly assignedClinic = computed(() => {
    const id = this.assignedClinicId();
    return id ? this.clinics().find((c) => c.id === id) || null : null;
  });

  // Selected Clinic for Queue / Finance filters
  protected selectedClinicId = '';

  // Appointment Filters
  protected filterDoctorId = '';
  protected filterStatus = '';
  protected filterSearchPatient = '';

  // Filtered Appointments
  protected readonly filteredAppointments = computed(() => {
    let list = this.appointments();

    if (this.filterDoctorId) {
      list = list.filter((a) => a.doctorId === this.filterDoctorId);
    }

    if (this.filterStatus) {
      list = list.filter((a) => a.status === this.filterStatus);
    }

    if (this.filterSearchPatient) {
      const q = this.filterSearchPatient.toLowerCase();
      list = list.filter(
        (a) =>
          a.patientName?.toLowerCase().includes(q) ||
          a.patientId.toLowerCase().includes(q) ||
          a.doctorName?.toLowerCase().includes(q)
      );
    }

    return list;
  });

  // --- Clinical Consultation Form State ---
  protected consultAppointmentId = '';
  protected consultChiefComplaint = '';
  protected consultExamination = '';
  protected consultDiagnosis = '';
  protected consultTreatmentPlan = '';
  protected consultFollowUpDate = '';
  protected consultLabRequests = '';
  // Vitals
  protected vitalsSystolic: number | null = 120;
  protected vitalsDiastolic: number | null = 80;
  protected vitalsHeartRate: number | null = 72;
  protected vitalsTemp: number | null = 37.0;
  protected vitalsWeight: number | null = 75;
  protected vitalsHeight: number | null = 175;
  protected vitalsGlucose: number | null = null;
  // Prescription Items
  protected rxMedications: {
    medicationName: string;
    dosage: string;
    frequency: string;
    duration: string;
    instructions: string;
  }[] = [
    { medicationName: 'Amoxicillin 500mg', dosage: '1 capsule', frequency: '3 times daily after meals', duration: '7 days', instructions: 'Take with full glass of water' }
  ];
  protected lastIssuedPrescription: PrescriptionDto | null = null;

  // --- Medical History Modal ---
  protected isHistoryModalOpen = false;
  protected historyBloodGroup = '';
  protected historyAllergies = '';
  protected historyChronic = '';
  protected historyNotes = '';

  // --- Rescheduling Modal State ---
  protected isRescheduleModalOpen = false;
  protected rescheduleAppt: AppointmentDto | null = null;
  protected rescheduleNewDate = '';
  protected rescheduleNewTime = '10:00';

  // --- Payment / Partial Payment Modal State ---
  protected isPayModalOpen = false;
  protected payingInvoice: InvoiceDto | null = null;
  protected payMethod: number = 1;
  protected payAmount: number | null = null;
  protected historyPatientId = '';

  // --- Audit Log State ---
  protected readonly auditLogs = signal<AuditLogDto[]>([]);
  protected auditSearch = '';
  protected auditEntityFilter = '';
  protected selectedAuditJson: string | null = null;
  protected isAuditJsonModalOpen = false;

  protected readonly filteredAuditLogs = computed(() => {
    let list = this.auditLogs();
    if (this.auditEntityFilter) {
      list = list.filter((l) => l.entityName.toLowerCase() === this.auditEntityFilter.toLowerCase());
    }
    if (this.auditSearch.trim()) {
      const q = this.auditSearch.toLowerCase();
      list = list.filter(
        (l) =>
          l.entityName.toLowerCase().includes(q) ||
          l.entityId.toLowerCase().includes(q) ||
          l.action.toLowerCase().includes(q) ||
          l.changedBy.toLowerCase().includes(q)
      );
    }
    return list;
  });

  // --- Sick Leave Certificate Modal State ---
  protected isSickLeaveModalOpen = false;
  protected sickLeavePatientName = '';
  protected sickLeaveDoctorName = '';
  protected sickLeaveClinicName = '';
  protected sickLeaveDiagnosis = '';
  protected sickLeaveStartDate = new Date().toISOString().slice(0, 10);
  protected sickLeaveDays = 3;
  protected sickLeaveRemarks = '';

  // --- Invoice Modal ---
  protected isInvoiceModalOpen = false;
  protected invPatientId = '';
  protected invClinicId = '';
  protected invDiscount = 0;
  protected invTax = 0;
  protected invMethod = 1; // 1 = Cash, 2 = Card, 3 = Insurance
  protected invItems: { description: string; quantity: number; unitPrice: number }[] = [
    { description: 'Specialist Consultation Fee', quantity: 1, unitPrice: 350 }
  ];

  // --- Clinic Services & Price List State ---
  protected billingViewMode = signal<'invoices' | 'priceList'>('invoices');
  protected readonly clinicServices = signal<ClinicServiceItem[]>([
    { id: '1', code: 'SRV-001', nameEn: 'General Practitioner Consultation', nameAr: 'كشف طبيب عام / ممارس', category: 'Consultation', price: 250 },
    { id: '2', code: 'SRV-002', nameEn: 'Consultant Specialist Examination', nameAr: 'كشف طبيب استشاري متخصص', category: 'Consultation', price: 450 },
    { id: '3', code: 'SRV-003', nameEn: 'Emergency Triage & Consultation', nameAr: 'كشف طوارئ وتقييم عاجل', category: 'Consultation', price: 350 },
    { id: '4', code: 'SRV-004', nameEn: 'Follow-up Consultation (Within 14 days)', nameAr: 'استشارة ومراجعة طبية (خلال 14 يوم)', category: 'Consultation', price: 100 },
    { id: '5', code: 'SRV-005', nameEn: '12-Lead Electrocardiogram (ECG)', nameAr: 'رسم وتخطيط قلب 12 قناة (ECG)', category: 'Diagnostic', price: 200 },
    { id: '6', code: 'SRV-006', nameEn: 'Abdominal & Pelvic Ultrasound', nameAr: 'موجات فوق صوتية سونار على البطن والحوض', category: 'Diagnostic', price: 500 },
    { id: '7', code: 'SRV-007', nameEn: 'Echocardiogram (Echo)', nameAr: 'أشعة إيكو على القلب', category: 'Diagnostic', price: 650 },
    { id: '8', code: 'SRV-008', nameEn: 'Complete Blood Picture (CBC)', nameAr: 'صورة دم كاملة (CBC)', category: 'Laboratory', price: 180 },
    { id: '9', code: 'SRV-009', nameEn: 'Fasting Blood Sugar & Lipid Profile', nameAr: 'تحليل سكر صائم ودهون الدم الكاملة', category: 'Laboratory', price: 220 },
    { id: '10', code: 'SRV-010', nameEn: 'HbA1c Glycated Hemoglobin', nameAr: 'تحليل سكر تراكمي HbA1c', category: 'Laboratory', price: 200 },
    { id: '11', code: 'SRV-011', nameEn: 'Kidney & Liver Function Panel', nameAr: 'وظائف كلى وكبد كاملة', category: 'Laboratory', price: 320 },
    { id: '12', code: 'SRV-012', nameEn: 'Minor Wound Debridement & Suture', nameAr: 'غيار وخياطة جروح بسيطة', category: 'Procedure', price: 300 },
    { id: '13', code: 'SRV-013', nameEn: 'Nebulizer Respiratory Session', nameAr: 'جلسة استنشاق موسع شعب (نيبولايزر)', category: 'Nursing', price: 120 },
    { id: '14', code: 'SRV-014', nameEn: 'IM / IV Medication Injection', nameAr: 'إعطاء حقنة عضلية أو وريدية', category: 'Nursing', price: 50 },
    { id: '15', code: 'SRV-015', nameEn: 'Dental Scaling & Polishing', nameAr: 'تنظيف وتلميع الأسنان وإزالة الجير', category: 'Dental', price: 400 }
  ]);
  protected serviceCategoryFilter = signal('');
  protected serviceSearch = signal('');

  protected readonly filteredClinicServices = computed(() => {
    let list = this.clinicServices();
    if (this.serviceCategoryFilter()) {
      list = list.filter((s) => s.category === this.serviceCategoryFilter());
    }
    if (this.serviceSearch().trim()) {
      const q = this.serviceSearch().toLowerCase();
      list = list.filter(
        (s) =>
          s.code.toLowerCase().includes(q) ||
          s.nameEn.toLowerCase().includes(q) ||
          s.nameAr.toLowerCase().includes(q) ||
          s.category.toLowerCase().includes(q)
      );
    }
    return list;
  });

  protected isAddServiceModalOpen = false;
  protected newServiceCode = '';
  protected newServiceNameEn = '';
  protected newServiceNameAr = '';
  protected newServiceCategory: 'Consultation' | 'Diagnostic' | 'Laboratory' | 'Procedure' | 'Dental' | 'Nursing' = 'Consultation';
  protected newServicePrice = 250;

  // --- Existing Modals ---
  protected isDoctorModalOpen = false;
  protected docFirstName = '';
  protected docLastName = '';
  protected docEmail = '';
  protected docPassword = 'Doctor@Secure123!';
  protected docSpecialtyId = '';
  protected docClinicId = '';
  protected docConsultationFee = 350;
  protected docLicenseNumber = '';
  protected docSlotDuration = 30;
  protected docBioEn = '';
  protected docBioAr = '';
  protected docInitialShiftDay = 0;
  protected docInitialShiftStart = '09:00:00';
  protected docInitialShiftEnd = '17:00:00';

  protected isEditClinicModalOpen = false;
  protected editingClinicId = '';
  protected editClinicNameEn = '';
  protected editClinicNameAr = '';
  protected editClinicAddressEn = '';
  protected editClinicAddressAr = '';
  protected editClinicCityEn = '';
  protected editClinicCityAr = '';
  protected editClinicPhone = '';
  protected editClinicIsActive = true;

  protected isSpecialtyModalOpen = false;
  protected specialtyNameEn = '';
  protected specialtyNameAr = '';
  protected specialtyDescEn = '';
  protected specialtyDescAr = '';

  protected isClinicModalOpen = false;
  protected clinicNameEn = '';
  protected clinicNameAr = '';
  protected clinicAddressEn = '';
  protected clinicAddressAr = '';
  protected clinicCityEn = 'Cairo';
  protected clinicCityAr = 'القاهرة';
  protected clinicPhone = '+201012345678';

  protected cancelingAppointmentId: string | null = null;
  protected cancelReason = 'Administrative schedule adjustment';

  // --- Staff & Permissions Form State ---
  protected newStaffEmail = '';
  protected newStaffPassword = '';
  protected newStaffFirstName = '';
  protected newStaffLastName = '';
  protected newStaffRole = 'Receptionist';
  protected newStaffClinicId = '';
  protected newStaffPermissions: string[] = [];

  protected editingStaffId = '';
  protected editingStaffFullName = '';
  protected editingStaffEmail = '';
  protected editingStaffRole = 'Receptionist';
  protected editingStaffClinicId = '';
  protected editingStaffPermissions: string[] = [];

  ngOnInit(): void {
    const allTabs: AdminTab[] = [
      'overview',
      'queue',
      'consultation',
      'records',
      'billing',
      'appointments',
      'doctors',
      'clinics',
      'specialties',
      'staff'
    ];

    if (!this.canAccessTab(this.activeTab())) {
      const firstAllowed = allTabs.find((t) => this.canAccessTab(t));
      if (firstAllowed) {
        this.activeTab.set(firstAllowed);
      }
    }

    this.loadAllData();
  }

  protected setTab(tab: AdminTab): void {
    this.activeTab.set(tab);
    if (tab === 'queue') {
      this.loadQueue();
    } else if (tab === 'billing') {
      this.loadInvoices();
      this.loadInsuranceProviders();
    } else if (tab === 'staff') {
      this.loadStaff();
      this.loadPermissionsCatalog();
    } else if (tab === 'inventory') {
      this.loadInventory();
    } else if (tab === 'records') {
      this.loadPatients();
    }
  }

  protected loadAllData(): void {
    this.loading.set(true);
    const clinicId = this.assignedClinicId() || undefined;

    this.loadPatients();

    this.adminService.getDashboardStats(clinicId).subscribe({
      next: (s) => this.stats.set(s),
      error: () => {}
    });

    this.adminService.getAllAppointments(undefined, undefined, undefined, clinicId).subscribe({
      next: (a) => {
        this.appointments.set(a);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });

    this.doctorService.getClinics().subscribe((c) => {
      this.clinics.set(c);
      if (c.length > 0 && !this.selectedClinicId) {
        this.selectedClinicId = this.assignedClinicId() || c[0].id;
        this.loadQueue();
        this.loadInvoices();
      }
    });

    this.doctorService.getSpecialties().subscribe((sp) => this.specialties.set(sp));
    this.doctorService.getDoctors(undefined, clinicId).subscribe((d) => {
      this.doctors.set(d);
      if (d.length > 0 && !this.selectedScheduleDoctorId()) {
        const user = this.auth.currentUser();
        const myDoc = d.find((doc) => doc.userId === user?.id);
        const targetDoc = myDoc || d[0];
        this.selectedScheduleDoctorId.set(targetDoc.id);
        if (targetDoc.doctorClinics && targetDoc.doctorClinics.length > 0) {
          this.scheduleClinicId = targetDoc.doctorClinics[0].clinicId;
          this.previewClinicId = targetDoc.doctorClinics[0].clinicId;
        } else if (this.clinics().length > 0) {
          this.scheduleClinicId = this.clinics()[0].id;
          this.previewClinicId = this.clinics()[0].id;
        }
      }
    });

    if (this.canAccessTab('staff')) {
      this.loadStaff();
      this.loadPermissionsCatalog();
    }

    if (this.canAccessTab('audit')) {
      this.loadAuditLogs();
    }

    if (this.canAccessTab('notifications')) {
      this.loadNotificationLogs();
    }
  }

  // --- Queue Actions ---
  protected openQueueTv(): void {
    window.open('/queue/tv', '_blank');
  }

  // --- Patients Directory & Registration Actions ---
  protected loadPatients(search?: string): void {
    this.clinicalService.getPatients(search).subscribe({
      next: (list) => this.patients.set(list),
      error: () => {}
    });
  }

  protected onSearchPatients(): void {
    this.loadPatients(this.patientSearchTerm);
  }

  protected openAddPatientModal(): void {
    this.patFirstName = '';
    this.patLastName = '';
    this.patEmail = '';
    this.patPhone = '';
    this.patDob = '';
    this.patGender = 'Male';
    this.patBloodGroup = '';
    this.patAllergies = '';
    this.patChronicConditions = '';
    this.patEmergencyContact = '';
    this.patPassword = '';
    this.isAddPatientModalOpen.set(true);
  }

  protected closeAddPatientModal(): void {
    this.isAddPatientModalOpen.set(false);
  }

  protected submitNewPatient(): void {
    if (!this.patFirstName.trim() || !this.patLastName.trim() || !this.patDob) {
      this.errorMessage.set(
        this.translation.isArabic()
          ? 'يرجى إدخال الاسم الأول، اسم العائلة، وتاريخ الميلاد للمريض.'
          : 'Please enter First Name, Last Name, and Date of Birth.'
      );
      return;
    }

    this.isSubmittingPatient.set(true);
    const req: CreatePatientRequest = {
      firstName: this.patFirstName.trim(),
      lastName: this.patLastName.trim(),
      email: this.patEmail.trim() || undefined,
      phoneNumber: this.patPhone.trim() || undefined,
      dateOfBirth: this.patDob,
      gender: this.patGender,
      bloodGroup: this.patBloodGroup || undefined,
      allergies: this.patAllergies.trim() || undefined,
      chronicConditions: this.patChronicConditions.trim() || undefined,
      emergencyContact: this.patEmergencyContact.trim() || undefined,
      password: this.patPassword.trim() || undefined
    };

    this.clinicalService.createPatient(req).subscribe({
      next: (patient) => {
        this.isSubmittingPatient.set(false);
        this.isAddPatientModalOpen.set(false);
        this.patients.update((prev) => [patient, ...prev]);
        this.showMessage(
          this.translation.isArabic()
            ? `تم تسجيل المريض "${patient.fullName}" بنجاح في المنظومة الطبية!`
            : `Patient "${patient.fullName}" registered successfully!`
        );
        this.viewPatientRecord(patient.id);
      },
      error: (err) => {
        this.isSubmittingPatient.set(false);
        this.errorMessage.set(
          err.error?.detail ||
            (this.translation.isArabic() ? 'فشل تسجيل المريض.' : 'Failed to register patient.')
        );
      }
    });
  }

  // --- Doctor Schedules & Holidays Actions ---
  protected openDoctorSchedule(doc: DoctorDto): void {
    this.selectedScheduleDoctorId.set(doc.id);
    if (doc.doctorClinics && doc.doctorClinics.length > 0) {
      this.scheduleClinicId = doc.doctorClinics[0].clinicId;
      this.previewClinicId = doc.doctorClinics[0].clinicId;
    } else if (this.clinics().length > 0) {
      this.scheduleClinicId = this.clinics()[0].id;
      this.previewClinicId = this.clinics()[0].id;
    }
    this.refreshDoctorDetails(doc.id);
    this.setTab('schedules');
  }

  protected openDoctorScheduleModal(doc: DoctorDto): void {
    this.selectedScheduleDoctorId.set(doc.id);
    if (doc.doctorClinics && doc.doctorClinics.length > 0) {
      this.scheduleClinicId = doc.doctorClinics[0].clinicId;
      this.previewClinicId = doc.doctorClinics[0].clinicId;
    } else if (this.clinics().length > 0) {
      this.scheduleClinicId = this.clinics()[0].id;
      this.previewClinicId = this.clinics()[0].id;
    }
    this.refreshDoctorDetails(doc.id);
    this.isScheduleModalOpen.set(true);
  }

  protected closeDoctorScheduleModal(): void {
    this.isScheduleModalOpen.set(false);
  }

  protected refreshDoctorDetails(doctorId: string): void {
    this.doctorService.getDoctorById(doctorId).subscribe({
      next: (fullDoc) => {
        this.doctors.update((prev) => prev.map((d) => (d.id === doctorId ? fullDoc : d)));
      },
      error: () => {}
    });
  }

  protected onSelectScheduleDoctor(doctorId: string): void {
    this.selectedScheduleDoctorId.set(doctorId);
    const doc = this.doctors().find((d) => d.id === doctorId);
    if (doc && doc.doctorClinics && doc.doctorClinics.length > 0) {
      this.scheduleClinicId = doc.doctorClinics[0].clinicId;
      this.previewClinicId = doc.doctorClinics[0].clinicId;
    }
    this.refreshDoctorDetails(doctorId);
  }

  protected saveDoctorAvailability(): void {
    const docId = this.selectedScheduleDoctorId();
    if (!docId || !this.scheduleClinicId) {
      this.errorMessage.set(
        this.translation.isArabic()
          ? 'يرجى اختيار الطبيب والفرع لإضافة المناوبة.'
          : 'Please select doctor and clinic branch.'
      );
      return;
    }

    if (this.scheduleStartTime >= this.scheduleEndTime) {
      this.errorMessage.set(
        this.translation.isArabic()
          ? 'وقت بداية المناوبة يجب أن يكون قبل وقت نهايتها.'
          : 'Shift start time must be earlier than end time.'
      );
      return;
    }

    this.isSavingSchedule.set(true);
    const formattedStart = this.scheduleStartTime.length === 5 ? `${this.scheduleStartTime}:00` : this.scheduleStartTime;
    const formattedEnd = this.scheduleEndTime.length === 5 ? `${this.scheduleEndTime}:00` : this.scheduleEndTime;

    const req: SetDoctorAvailabilityRequest = {
      clinicId: this.scheduleClinicId,
      dayOfWeek: Number(this.scheduleDayOfWeek),
      startTime: formattedStart,
      endTime: formattedEnd
    };

    this.doctorService.setDoctorAvailability(docId, req).subscribe({
      next: () => {
        this.isSavingSchedule.set(false);
        this.showMessage(
          this.translation.isArabic()
            ? 'تم إضافة وردية العمل الأسبوعية بنجاح!'
            : 'Weekly shift scheduled successfully!'
        );
        this.refreshDoctorDetails(docId);
      },
      error: (err) => {
        this.isSavingSchedule.set(false);
        this.errorMessage.set(
          err.error?.detail ||
            (this.translation.isArabic()
              ? 'فشل إضافة الوردية أو يوجد تعارض في ساعات العمل.'
              : 'Failed to add shift or time overlap.')
        );
      }
    });
  }

  protected deleteDoctorAvailability(availabilityId: string): void {
    const docId = this.selectedScheduleDoctorId();
    if (!docId || !availabilityId) return;

    this.doctorService.removeDoctorAvailability(docId, availabilityId).subscribe({
      next: () => {
        this.showMessage(
          this.translation.isArabic() ? 'تم إلغاء وردية العمل بنجاح.' : 'Shift removed successfully.'
        );
        this.refreshDoctorDetails(docId);
      },
      error: () =>
        this.errorMessage.set(
          this.translation.isArabic() ? 'فشل إلغاء الوردية.' : 'Failed to delete shift.'
        )
    });
  }

  protected saveDoctorHolidayException(): void {
    const docId = this.selectedScheduleDoctorId();
    if (!docId || !this.holidayStartDate || !this.holidayEndDate || !this.holidayReason.trim()) {
      this.errorMessage.set(
        this.translation.isArabic()
          ? 'يرجى تحديد فترة الإجازة الرسمية (من/إلى) وكتابة سببها.'
          : 'Please enter holiday start, end, and reason.'
      );
      return;
    }

    if (new Date(this.holidayStartDate) >= new Date(this.holidayEndDate)) {
      this.errorMessage.set(
        this.translation.isArabic()
          ? 'تاريخ بداية الإجازة يجب أن يكون قبل تاريخ نهايتها.'
          : 'Holiday start date must be earlier than end date.'
      );
      return;
    }

    this.isSavingHoliday.set(true);
    const req: AddAvailabilityExceptionRequest = {
      startUtc: new Date(this.holidayStartDate).toISOString(),
      endUtc: new Date(this.holidayEndDate).toISOString(),
      reason: this.holidayReason.trim()
    };

    this.doctorService.addAvailabilityException(docId, req).subscribe({
      next: () => {
        this.isSavingHoliday.set(false);
        this.holidayStartDate = '';
        this.holidayEndDate = '';
        this.showMessage(
          this.translation.isArabic()
            ? 'تم اعتماد استثناء الإجازة/العطلة الرسمية وحظر الحجز في هذه الفترة!'
            : 'Holiday exception added and booking blocked successfully!'
        );
        this.refreshDoctorDetails(docId);
      },
      error: (err) => {
        this.isSavingHoliday.set(false);
        this.errorMessage.set(
          err.error?.detail ||
            (this.translation.isArabic()
              ? 'فشل تسجيل الإجازة.'
              : 'Failed to record holiday exception.')
        );
      }
    });
  }

  protected deleteDoctorHolidayException(exceptionId: string): void {
    const docId = this.selectedScheduleDoctorId();
    if (!docId || !exceptionId) return;

    this.doctorService.removeAvailabilityException(docId, exceptionId).subscribe({
      next: () => {
        this.showMessage(
          this.translation.isArabic()
            ? 'تم إزالة استثناء الإجازة واستئناف العمل بشكل طبيعي.'
            : 'Holiday exception removed.'
        );
        this.refreshDoctorDetails(docId);
      },
      error: () =>
        this.errorMessage.set(
          this.translation.isArabic() ? 'فشل إزالة الإجازة.' : 'Failed to remove holiday exception.'
        )
    });
  }

  protected testAvailableSlots(): void {
    const docId = this.selectedScheduleDoctorId();
    if (!docId || !this.previewClinicId || !this.previewSlotDate) {
      this.previewSlotStatusMessage.set(
        this.translation.isArabic()
          ? 'يرجى اختيار الفرع والتاريخ لفحص المواعيد المتاحة.'
          : 'Please choose clinic and date.'
      );
      return;
    }

    this.isCheckingSlots.set(true);
    this.previewSlotStatusMessage.set(null);
    this.previewSlots.set([]);

    this.doctorService.getAvailableSlots(docId, this.previewClinicId, this.previewSlotDate).subscribe({
      next: (slots) => {
        this.isCheckingSlots.set(false);
        this.previewSlots.set(slots);
        if (slots.length === 0) {
          const doc = this.selectedScheduleDoctor();
          const targetDate = new Date(this.previewSlotDate);
          const dayOfWeek = targetDate.getDay();
          const isWeekend = dayOfWeek === 5 || dayOfWeek === 6;
          const isException = doc?.exceptions?.some((e) => {
            const s = new Date(e.startUtc);
            const en = new Date(e.endUtc);
            return targetDate >= s && targetDate <= en;
          });

          if (isException) {
            this.previewSlotStatusMessage.set(
              this.translation.isArabic()
                ? '🛑 لا توجد مواعيد: هذا التاريخ مسجل كإجازة رسمية / عطلة محظورة للطبيب.'
                : '🛑 No slots: This date is blocked as an official holiday / exception.'
            );
          } else if (isWeekend) {
            this.previewSlotStatusMessage.set(
              this.translation.isArabic()
                ? '🌴 لا توجد مواعيد: هذا التاريخ عطلة أسبوعية للطبيب (لم تسجل وردية عمل يوم الجمعة أو السبت).'
                : '🌴 No slots: Weekend day with no active shift scheduled.'
            );
          } else {
            this.previewSlotStatusMessage.set(
              this.translation.isArabic()
                ? 'ℹ️ لا توجد شرائح مواعيد متاحة في هذا اليوم (قد تكون جميعها محجوزة أو خارج ساعات العمل).'
                : 'ℹ️ No available slots on this day.'
            );
          }
        }
      },
      error: (err) => {
        this.isCheckingSlots.set(false);
        this.previewSlotStatusMessage.set(
          err.error?.detail ||
            (this.translation.isArabic() ? 'تعذر جلب المواعيد.' : 'Failed to query slots.')
        );
      }
    });
  }

  // --- Staff / Reception Booking Actions ---
  protected openBookAppointmentModal(patientId?: string, doctorId?: string): void {
    this.bookPatientId = patientId || (this.patients().length > 0 ? this.patients()[0].id : '');
    this.bookDoctorId = doctorId || (this.doctors().length > 0 ? this.doctors()[0].id : '');
    this.bookClinicId = this.assignedClinicId() || (this.clinics().length > 0 ? this.clinics()[0].id : '');

    const tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 1);
    this.bookDate = tomorrow.toISOString().split('T')[0];

    this.bookSelectedSlot = null;
    this.bookNotes = '';
    this.bookImmediateCheckIn = false;
    this.bookSlots.set([]);
    this.bookSlotsError.set(null);
    this.isBookAppointmentModalOpen.set(true);

    if (this.bookDoctorId && this.bookClinicId && this.bookDate) {
      this.loadBookSlots();
    }
  }

  protected closeBookAppointmentModal(): void {
    this.isBookAppointmentModalOpen.set(false);
  }

  protected loadBookSlots(): void {
    if (!this.bookDoctorId || !this.bookClinicId || !this.bookDate) {
      this.bookSlots.set([]);
      return;
    }

    this.isLoadingBookSlots.set(true);
    this.bookSlotsError.set(null);
    this.bookSelectedSlot = null;

    this.doctorService.getAvailableSlots(this.bookDoctorId, this.bookClinicId, this.bookDate).subscribe({
      next: (slots) => {
        this.isLoadingBookSlots.set(false);
        this.bookSlots.set(slots);
        if (slots.length === 0) {
          const doc = this.doctors().find((d) => d.id === this.bookDoctorId);
          const targetDate = new Date(this.bookDate);
          const dayOfWeek = targetDate.getDay();
          const isWeekend = dayOfWeek === 5 || dayOfWeek === 6;
          const isBlocked = doc?.exceptions?.some((e) => {
            const s = new Date(e.startUtc);
            const en = new Date(e.endUtc);
            return targetDate >= s && targetDate <= en;
          });

          if (isBlocked) {
            this.bookSlotsError.set(
              this.translation.isArabic()
                ? '🛑 لا يمكن الحجز: هذا التاريخ مسجل كإجازة رسمية / عطلة محظورة للطبيب.'
                : '🛑 Cannot book: Doctor has an official holiday on this date.'
            );
          } else if (isWeekend) {
            this.bookSlotsError.set(
              this.translation.isArabic()
                ? '🌴 لا يمكن الحجز: هذا التاريخ عطلة أسبوعية للطبيب ولا توجد وردية عمل مسجلة.'
                : '🌴 Cannot book: Weekend day with no active working shift scheduled.'
            );
          } else {
            this.bookSlotsError.set(
              this.translation.isArabic()
                ? 'ℹ️ لا توجد شرائح مواعيد شاغرة في هذا اليوم.'
                : 'ℹ️ No available slots on this date.'
            );
          }
        }
      },
      error: (err) => {
        this.isLoadingBookSlots.set(false);
        this.bookSlots.set([]);
        this.bookSlotsError.set(
          err.error?.detail ||
            (this.translation.isArabic() ? 'تعذر تحميل المواعيد المتاحة.' : 'Failed to load slots.')
        );
      }
    });
  }

  protected selectBookSlot(slot: AvailableSlotDto): void {
    if (!slot.isAvailable) return;
    this.bookSelectedSlot = slot;
  }

  protected submitBookAppointment(): void {
    if (!this.bookPatientId || !this.bookDoctorId || !this.bookClinicId || !this.bookSelectedSlot) {
      this.errorMessage.set(
        this.translation.isArabic()
          ? 'يرجى اختيار المريض والطبيب والفرع وتحديد شريحة الموعد المتاحة.'
          : 'Please select patient, doctor, clinic, and appointment slot.'
      );
      return;
    }

    const doc = this.doctors().find((d) => d.id === this.bookDoctorId);
    const tenantId = doc?.tenantId || (this.auth.currentUser()?.tenantId ?? '');

    this.isBookingAppointment.set(true);
    this.appointmentService
      .bookAppointment({
        tenantId,
        doctorId: this.bookDoctorId,
        patientId: this.bookPatientId,
        clinicId: this.bookClinicId,
        startTimeUtc: this.bookSelectedSlot.startTimeUtc,
        endTimeUtc: this.bookSelectedSlot.endTimeUtc,
        notes: this.bookNotes.trim() || undefined
      })
      .subscribe({
        next: (appt) => {
          this.isBookingAppointment.set(false);
          this.isBookAppointmentModalOpen.set(false);
          this.showMessage(
            this.translation.isArabic()
              ? 'تم حجز الموعد بنجاح للمريض!'
              : 'Appointment successfully booked for patient!'
          );
          this.loadAllData();

          if (this.bookImmediateCheckIn && appt?.id) {
            this.checkInPatient(appt.id);
          }
        },
        error: (err) => {
          this.isBookingAppointment.set(false);
          this.errorMessage.set(
            err.error?.detail ||
              (this.translation.isArabic()
                ? 'فشل حجز الموعد، يرجى المحاولة مرة أخرى.'
                : 'Booking failed. Please try again.')
          );
        }
      });
  }

  protected loadQueue(): void {
    const clinicId = this.assignedClinicId() || this.selectedClinicId;
    if (!clinicId) return;

    this.clinicalService.getClinicQueue(clinicId).subscribe({
      next: (items) => this.queueItems.set(items),
      error: () => {}
    });
  }

  protected checkInPatient(appointmentId: string): void {
    this.clinicalService.checkIn(appointmentId).subscribe({
      next: (item) => {
        this.showMessage(`Patient checked in! Assigned Queue Token #${item.queueNumber}`);
        this.loadQueue();
        this.loadAllData();
      },
      error: (err) => this.errorMessage.set(err.error?.detail || 'Failed to check in patient.')
    });
  }

  protected callPatient(appointmentId: string): void {
    this.clinicalService.callPatient(appointmentId).subscribe({
      next: (item) => {
        this.showMessage(`Patient called into examination room! Status updated to In-Consultation.`);
        this.loadQueue();
        this.loadAllData();
      },
      error: (err) => this.errorMessage.set(err.error?.detail || 'Failed to call patient.')
    });
  }

  protected openConsultationForAppt(appt: AppointmentDto | QueueItemDto): void {
    this.consultAppointmentId = 'appointmentId' in appt ? appt.appointmentId : appt.id;
    this.setTab('consultation');
  }

  // --- Consultation & Prescription Actions ---
  protected addMedicationRow(): void {
    this.rxMedications.push({
      medicationName: '',
      dosage: '',
      frequency: 'Once daily',
      duration: '5 days',
      instructions: ''
    });
  }

  protected removeMedicationRow(index: number): void {
    if (this.rxMedications.length > 1) {
      this.rxMedications.splice(index, 1);
    }
  }

  protected submitConsultation(): void {
    if (!this.consultAppointmentId || !this.consultDiagnosis) {
      this.errorMessage.set('Please select an appointment and provide a clinical diagnosis.');
      return;
    }

    const appt = this.appointments().find((a) => a.id === this.consultAppointmentId);
    if (!appt) {
      this.errorMessage.set('Appointment not found.');
      return;
    }

    const tenantId = this.auth.currentUser()?.tenantId || '1f2aa19b-f5dc-4166-abe6-dc93a2f3aaff';

    const command = {
      tenantId,
      appointmentId: appt.id,
      patientId: appt.patientId,
      doctorId: appt.doctorId,
      clinicId: appt.clinicId,
      chiefComplaint: this.consultChiefComplaint,
      examinationNotes: this.consultExamination,
      diagnosis: this.consultDiagnosis,
      treatmentPlan: this.consultTreatmentPlan,
      followUpDateUtc: this.consultFollowUpDate ? new Date(this.consultFollowUpDate).toISOString() : undefined,
      labRequests: this.consultLabRequests.trim() || undefined,
      vitalSigns: {
        systolicBp: this.vitalsSystolic ?? undefined,
        diastolicBp: this.vitalsDiastolic ?? undefined,
        heartRate: this.vitalsHeartRate ?? undefined,
        temperatureCelsius: this.vitalsTemp ?? undefined,
        weightKg: this.vitalsWeight ?? undefined,
        heightCm: this.vitalsHeight ?? undefined,
        bloodGlucoseMgDl: this.vitalsGlucose ?? undefined
      }
    };

    this.clinicalService.createConsultation(command).subscribe({
      next: (consult) => {
        // If medications entered, generate official prescription
        const validMeds = this.rxMedications.filter((m) => m.medicationName.trim().length > 0);
        if (validMeds.length > 0) {
          this.clinicalService
            .createPrescription({
              tenantId,
              consultationId: consult.id,
              patientId: appt.patientId,
              doctorId: appt.doctorId,
              clinicId: appt.clinicId,
              diagnosis: this.consultDiagnosis,
              generalInstructions: this.consultTreatmentPlan,
              items: validMeds
            })
            .subscribe({
              next: (rx) => {
                this.lastIssuedPrescription = rx;
                this.showMessage('Clinical consultation and Electronic Prescription (Rx) created successfully!');
                this.loadAllData();
                this.loadQueue();
              }
            });
        } else {
          this.showMessage('Clinical consultation recorded and visit completed successfully!');
          this.loadAllData();
          this.loadQueue();
        }
      },
      error: (err) => this.errorMessage.set(err.error?.detail || 'Failed to record consultation.')
    });
  }

  // --- Patient Medical Records (EMR) Actions ---
  protected viewPatientRecord(patientId: string): void {
    this.clinicalService.getPatientMedicalRecord(patientId).subscribe({
      next: (rec) => {
        this.patientRecord.set(rec);
        this.loadPatientAttachments(patientId);
        this.setTab('records');
      },
      error: (err) => this.errorMessage.set(err.error?.detail || 'Failed to load patient medical record.')
    });
  }

  protected openEditHistoryModal(rec: PatientMedicalRecordDto): void {
    this.isHistoryModalOpen = true;
    this.historyPatientId = rec.patientId;
    this.historyBloodGroup = rec.bloodGroup || 'O+';
    this.historyAllergies = rec.allergies || '';
    this.historyChronic = rec.chronicConditions || '';
    this.historyNotes = rec.medicalNotes || '';
  }

  protected closeHistoryModal(): void {
    this.isHistoryModalOpen = false;
  }

  protected submitMedicalHistory(): void {
    if (!this.historyPatientId) return;

    this.clinicalService
      .updateMedicalHistory(this.historyPatientId, {
        bloodGroup: this.historyBloodGroup,
        allergies: this.historyAllergies,
        chronicConditions: this.historyChronic,
        medicalNotes: this.historyNotes
      })
      .subscribe({
        next: () => {
          this.closeHistoryModal();
          this.viewPatientRecord(this.historyPatientId);
          this.showMessage('Patient medical background updated successfully!');
        },
        error: (err) => this.errorMessage.set(err.error?.detail || 'Failed to update medical history.')
      });
  }

  // --- Billing & Invoicing Actions ---
  protected loadInvoices(): void {
    const clinicId = this.assignedClinicId() || this.selectedClinicId;
    this.clinicalService.getInvoices(clinicId).subscribe({
      next: (invs) => this.invoices.set(invs),
      error: () => {}
    });
  }

  protected openInvoiceModal(): void {
    this.isInvoiceModalOpen = true;
    this.invPatientId = this.appointments().length > 0 ? this.appointments()[0].patientId : '';
    this.invClinicId = this.assignedClinicId() || (this.clinics().length > 0 ? this.clinics()[0].id : '');
    this.invDiscount = 0;
    this.invTax = 0;
    this.invMethod = 1; // Cash
    this.invInsuranceProviderId = this.insuranceProviders().length > 0 ? this.insuranceProviders()[0].id : '';
    this.invItems = [{ description: 'Specialist Consultation Fee', quantity: 1, unitPrice: 350 }];
    this.recalculateInsuranceShares();
  }

  protected recalculateInsuranceShares(): void {
    const total = this.invItems.reduce((acc, it) => acc + (it.quantity * it.unitPrice), 0) - this.invDiscount + this.invTax;
    if (Number(this.invMethod) === 3 && this.invInsuranceProviderId) {
      const provider = this.insuranceProviders().find((p) => p.id === this.invInsuranceProviderId);
      const coverageRate = provider ? (provider.defaultCoveragePercentage / 100) : 0.8;
      this.invInsuranceCoverageAmount = Math.round(total * coverageRate * 100) / 100;
      this.invPatientCopayAmount = Math.round((total - this.invInsuranceCoverageAmount) * 100) / 100;
    } else {
      this.invInsuranceCoverageAmount = 0;
      this.invPatientCopayAmount = Math.max(0, total);
    }
  }

  protected closeInvoiceModal(): void {
    this.isInvoiceModalOpen = false;
  }

  protected addInvoiceItemRow(): void {
    this.invItems.push({ description: '', quantity: 1, unitPrice: 100 });
    this.recalculateInsuranceShares();
  }

  protected removeInvoiceItemRow(index: number): void {
    if (this.invItems.length > 1) {
      this.invItems.splice(index, 1);
      this.recalculateInsuranceShares();
    }
  }

  protected submitInvoice(): void {
    if (!this.invPatientId || !this.invClinicId || this.invItems.length === 0) return;

    const tenantId = this.auth.currentUser()?.tenantId || '1f2aa19b-f5dc-4166-abe6-dc93a2f3aaff';
    const isInsurance = Number(this.invMethod) === 3;

    this.clinicalService
      .createInvoice({
        tenantId,
        patientId: this.invPatientId,
        clinicId: this.invClinicId,
        discountAmount: this.invDiscount,
        taxAmount: this.invTax,
        paymentMethod: Number(this.invMethod),
        insuranceProviderId: isInsurance && this.invInsuranceProviderId ? this.invInsuranceProviderId : undefined,
        insuranceCoverageAmount: isInsurance ? this.invInsuranceCoverageAmount : 0,
        patientCopayAmount: isInsurance ? this.invPatientCopayAmount : 0,
        items: this.invItems
      })
      .subscribe({
        next: (inv) => {
          this.closeInvoiceModal();
          this.invoices.update((prev) => [inv, ...prev]);
          this.showMessage(`Invoice #${inv.invoiceNumber} generated successfully!`);
        },
        error: (err) => this.errorMessage.set(err.error?.detail || 'Failed to create invoice.')
      });
  }

  protected payInvoice(invoiceId: string, method: number = 1): void {
    this.clinicalService.payInvoice(invoiceId, method).subscribe({
      next: (updated) => {
        this.invoices.update((prev) => prev.map((i) => (i.id === updated.id ? updated : i)));
        this.showMessage(`Invoice #${updated.invoiceNumber} payment processed!`);
      },
      error: (err) => this.errorMessage.set(err.error?.detail || 'Failed to pay invoice.')
    });
  }

  // --- Rescheduling Modal Methods ---
  protected openRescheduleModal(appt: AppointmentDto): void {
    this.rescheduleAppt = appt;
    const current = new Date(appt.startTimeUtc);
    this.rescheduleNewDate = current.toISOString().slice(0, 10);
    this.rescheduleNewTime = `${String(current.getHours()).padStart(2, '0')}:${String(current.getMinutes()).padStart(2, '0')}`;
    this.isRescheduleModalOpen = true;
  }

  protected submitReschedule(): void {
    if (!this.rescheduleAppt || !this.rescheduleNewDate || !this.rescheduleNewTime) {
      this.errorMessage.set('Please select both a new date and start time.');
      return;
    }

    const [hours, minutes] = this.rescheduleNewTime.split(':').map(Number);
    const start = new Date(this.rescheduleNewDate);
    start.setHours(hours, minutes, 0, 0);
    const end = new Date(start.getTime() + 30 * 60000);

    this.appointmentService.rescheduleAppointment(
      this.rescheduleAppt.id,
      start.toISOString(),
      end.toISOString()
    ).subscribe({
      next: () => {
        this.showMessage(`Appointment successfully rescheduled to ${start.toLocaleString()}!`);
        this.isRescheduleModalOpen = false;
        this.rescheduleAppt = null;
        this.loadAllData();
      },
      error: (err) => this.errorMessage.set(err.error?.detail || 'Failed to reschedule appointment.')
    });
  }

  // --- Payment / Partial Payment Methods ---
  protected openPayModal(inv: InvoiceDto): void {
    this.payingInvoice = inv;
    const remaining = (inv.remainingAmount !== undefined && inv.remainingAmount > 0)
      ? inv.remainingAmount
      : Math.max(0, inv.totalAmount - inv.paidAmount);
    this.payAmount = remaining > 0 ? remaining : inv.totalAmount;
    this.payMethod = 1;
    this.isPayModalOpen = true;
  }

  protected submitPayment(): void {
    if (!this.payingInvoice) return;
    const amount = this.payAmount !== null && this.payAmount > 0 ? this.payAmount : undefined;

    this.clinicalService.payInvoice(this.payingInvoice.id, this.payMethod, amount).subscribe({
      next: (updated) => {
        this.invoices.update((prev) => prev.map((i) => (i.id === updated.id ? updated : i)));
        this.isPayModalOpen = false;
        this.payingInvoice = null;
        const remaining = updated.remainingAmount ?? Math.max(0, updated.totalAmount - updated.paidAmount);
        const msg = updated.status === 'Paid'
          ? `Invoice #${updated.invoiceNumber} paid in full!`
          : `Partial payment recorded for Invoice #${updated.invoiceNumber}. Remaining balance: $${remaining}.`;
        this.showMessage(msg);
      },
      error: (err) => this.errorMessage.set(err.error?.detail || 'Failed to process payment.')
    });
  }

  protected printLabOrder(consultation?: ConsultationDto): void {
    if (consultation) {
      this.reporting.printLabOrderRequisition(
        consultation.patientName || 'Patient',
        consultation.doctorName,
        consultation.clinicName,
        consultation.diagnosis,
        consultation.labRequests,
        consultation.followUpDateUtc
      );
      return;
    }
    const appt = this.appointments().find((a) => a.id === this.consultAppointmentId);
    this.reporting.printLabOrderRequisition(
      appt?.patientName || 'Patient',
      appt?.doctorName,
      appt?.clinicName,
      this.consultDiagnosis,
      this.consultLabRequests,
      this.consultFollowUpDate
    );
  }

  // --- Audit Logs Methods ---
  protected loadAuditLogs(): void {
    this.adminService.getAuditLogs(undefined, 150).subscribe({
      next: (logs) => this.auditLogs.set(logs),
      error: () => {}
    });
  }

  protected viewAuditChanges(log: AuditLogDto): void {
    if (!log.changesJson) {
      this.selectedAuditJson = this.translation.isArabic()
        ? 'لا توجد تفاصيل إضافية مسجلة لهذه العملية.'
        : 'No property diff recorded for this operation.';
    } else {
      try {
        const parsed = JSON.parse(log.changesJson);
        this.selectedAuditJson = JSON.stringify(parsed, null, 2);
      } catch {
        this.selectedAuditJson = log.changesJson;
      }
    }
    this.isAuditJsonModalOpen = true;
  }

  protected printAuditLogs(): void {
    this.reporting.printAuditLogReport(this.filteredAuditLogs(), this.getActiveClinicName());
  }

  protected exportAuditLogsCsv(): void {
    this.reporting.exportAuditLogsCsv(this.filteredAuditLogs());
  }

  // --- Sick Leave Certificate Methods ---
  protected openSickLeaveModal(consultation?: ConsultationDto): void {
    const user = this.auth.currentUser();
    const fallbackDoc = user ? `${user.firstName} ${user.lastName}`.trim() : 'Doctor';

    if (consultation) {
      this.sickLeavePatientName = consultation.patientName || 'Patient';
      this.sickLeaveDoctorName = consultation.doctorName || fallbackDoc;
      this.sickLeaveClinicName = consultation.clinicName || this.getActiveClinicName();
      this.sickLeaveDiagnosis = consultation.diagnosis || '';
    } else {
      const appt = this.appointments().find((a) => a.id === this.consultAppointmentId);
      this.sickLeavePatientName = appt?.patientName || 'Patient';
      this.sickLeaveDoctorName = appt?.doctorName || fallbackDoc;
      this.sickLeaveClinicName = appt?.clinicName || this.getActiveClinicName();
      this.sickLeaveDiagnosis = this.consultDiagnosis || '';
    }
    this.sickLeaveStartDate = new Date().toISOString().slice(0, 10);
    this.sickLeaveDays = 3;
    this.sickLeaveRemarks = this.translation.isArabic()
      ? 'راحة تامة وتجنب الإجهاد البدني والمتابعة حال استمرار الأعراض.'
      : 'Strict rest advised; avoid physical exertion and follow up if symptoms persist.';
    this.isSickLeaveModalOpen = true;
  }

  protected submitSickLeave(): void {
    this.reporting.printSickLeaveCertificate(
      this.sickLeavePatientName,
      this.sickLeaveDoctorName,
      this.sickLeaveClinicName,
      this.sickLeaveDiagnosis,
      this.sickLeaveStartDate,
      this.sickLeaveDays,
      this.sickLeaveRemarks
    );
    this.isSickLeaveModalOpen = false;
    this.showMessage('Official Medical Sick Leave Certificate generated and ready for print!');
  }

  // --- Clinic Services & Procedures Methods ---
  protected setBillingView(mode: 'invoices' | 'priceList'): void {
    this.billingViewMode.set(mode);
  }

  protected addServiceToInvoice(srv: ClinicServiceItem): void {
    const desc = this.translation.isArabic() && srv.nameAr ? `${srv.nameAr} (${srv.nameEn})` : srv.nameEn;
    this.invItems.push({
      description: desc,
      quantity: 1,
      unitPrice: srv.price
    });
    this.showMessage(`Added "${srv.nameEn}" (${srv.price} EGP) to invoice items.`);
  }

  protected openAddServiceModal(): void {
    const nextNum = this.clinicServices().length + 1;
    this.newServiceCode = `SRV-${String(nextNum).padStart(3, '0')}`;
    this.newServiceNameEn = '';
    this.newServiceNameAr = '';
    this.newServiceCategory = 'Consultation';
    this.newServicePrice = 200;
    this.isAddServiceModalOpen = true;
  }

  protected submitNewService(): void {
    if (!this.newServiceNameEn.trim() || this.newServicePrice <= 0) {
      this.errorMessage.set('Please provide service name and positive price.');
      return;
    }
    const srv: ClinicServiceItem = {
      id: String(Date.now()),
      code: this.newServiceCode.trim() || `SRV-${this.clinicServices().length + 1}`,
      nameEn: this.newServiceNameEn.trim(),
      nameAr: this.newServiceNameAr.trim() || this.newServiceNameEn.trim(),
      category: this.newServiceCategory,
      price: this.newServicePrice
    };
    this.clinicServices.update((prev) => [...prev, srv]);
    this.isAddServiceModalOpen = false;
    this.showMessage(`Medical service "${srv.nameEn}" added to price list.`);
  }

  protected printServicesPriceList(): void {
    this.reporting.printServicesPriceList(this.filteredClinicServices(), this.getActiveClinicName());
  }

  protected exportServicesPriceListCsv(): void {
    this.reporting.exportServicesPriceListCsv(this.filteredClinicServices());
  }

  // --- Vitals Clinical Assessments ---
  protected getBpAssessment(systolic?: number, diastolic?: number): { textAr: string; textEn: string; class: string } {
    if (!systolic || !diastolic) return { textAr: 'غير محدد', textEn: 'Not Recorded', class: 'badge-scheduled' };
    if (systolic < 120 && diastolic < 80) return { textAr: 'طبيعي ومثالي', textEn: 'Optimal Normal', class: 'badge-confirmed' };
    if (systolic >= 140 || diastolic >= 90) return { textAr: 'ارتفاع ضغط دم (مرحلة 2)', textEn: 'Hypertension Stage 2', class: 'badge-cancelled' };
    if (systolic >= 130 || diastolic >= 80) return { textAr: 'ارتفاع ضغط دم (مرحلة 1)', textEn: 'Hypertension Stage 1', class: 'badge-warning' };
    return { textAr: 'مرتفع طفيفاً', textEn: 'Elevated', class: 'badge-in-consultation' };
  }

  protected getGlucoseAssessment(glucose?: number): { textAr: string; textEn: string; class: string } {
    if (!glucose) return { textAr: 'غير مقاس', textEn: 'Not Tested', class: 'badge-scheduled' };
    if (glucose < 100) return { textAr: 'طبيعي (صائم)', textEn: 'Normal Fasting', class: 'badge-confirmed' };
    if (glucose >= 126) return { textAr: 'مرتفع (سكري)', textEn: 'Diabetic Range', class: 'badge-cancelled' };
    return { textAr: 'مرحلة ما قبل السكري', textEn: 'Pre-diabetes', class: 'badge-warning' };
  }

  protected getBmiAssessment(bmi?: number): { textAr: string; textEn: string; class: string } {
    if (!bmi) return { textAr: 'غير محسوب', textEn: 'N/A', class: 'badge-scheduled' };
    if (bmi < 18.5) return { textAr: 'نقص في الوزن', textEn: 'Underweight', class: 'badge-warning' };
    if (bmi < 25) return { textAr: 'وزن صحي مثالي', textEn: 'Healthy Weight', class: 'badge-confirmed' };
    if (bmi < 30) return { textAr: 'زيادة في الوزن', textEn: 'Overweight', class: 'badge-in-consultation' };
    return { textAr: 'سمنة سريرية', textEn: 'Obese', class: 'badge-cancelled' };
  }

  protected getHeartRateAssessment(heartRate?: number): { textAr: string; textEn: string; class: string } {
    if (!heartRate) return { textAr: 'غير مقاس', textEn: 'Not Recorded', class: 'badge-scheduled' };
    if (heartRate < 60) return { textAr: 'بطء نبض (Bradycardia)', textEn: 'Bradycardia (<60)', class: 'badge-warning' };
    if (heartRate <= 100) return { textAr: 'طبيعي ومستقر', textEn: 'Normal (60-100)', class: 'badge-confirmed' };
    return { textAr: 'تسارع نبض (Tachycardia)', textEn: 'Tachycardia (>100)', class: 'badge-cancelled' };
  }

  // --- Doctor Actions ---
  protected openDoctorModal(): void {
    this.isDoctorModalOpen = true;
    this.docFirstName = '';
    this.docLastName = '';
    this.docEmail = '';
    this.docPassword = 'Doctor@Secure123!';
    this.docSpecialtyId = this.specialties().length > 0 ? this.specialties()[0].id : '';
    this.docClinicId = this.assignedClinicId() || (this.clinics().length > 0 ? this.clinics()[0].id : '');
    this.docConsultationFee = 350;
    this.docLicenseNumber = 'EGY-MED-' + Math.floor(10000 + Math.random() * 90000);
    this.docSlotDuration = 30;
    this.docBioEn = 'Senior Consultant with extensive clinical care experience.';
    this.docBioAr = 'استشاري أول متخصص يتمتع بخبرة سريرية واسعة.';
    this.docInitialShiftDay = 0;
    this.docInitialShiftStart = '09:00:00';
    this.docInitialShiftEnd = '17:00:00';
  }

  protected closeDoctorModal(): void {
    this.isDoctorModalOpen = false;
  }

  protected submitDoctor(): void {
    if (!this.docFirstName || !this.docLastName || !this.docEmail || !this.docSpecialtyId || !this.docLicenseNumber) {
      return;
    }

    const tenantId = this.auth.currentUser()?.tenantId || '1f2aa19b-f5dc-4166-abe6-dc93a2f3aaff';
    const clinicId = this.assignedClinicId() || this.docClinicId || undefined;

    const request: RegisterDoctorRequest = {
      tenantId,
      firstName: this.docFirstName,
      lastName: this.docLastName,
      email: this.docEmail,
      password: this.docPassword,
      specialtyId: this.docSpecialtyId,
      licenseNumber: this.docLicenseNumber,
      bioEn: this.docBioEn,
      bioAr: this.docBioAr || undefined,
      clinicId,
      consultationFee: this.docConsultationFee,
      defaultSlotDurationMinutes: this.docSlotDuration,
      requiresApproval: false,
      initialShiftDay: this.docInitialShiftDay,
      initialShiftStartTime: this.docInitialShiftStart,
      initialShiftEndTime: this.docInitialShiftEnd
    };

    this.adminService.registerDoctor(request).subscribe({
      next: (created) => {
        this.closeDoctorModal();
        this.doctors.update((prev) => [created, ...prev]);
        this.showMessage(`Doctor Dr. ${created.fullName || this.docFirstName + ' ' + this.docLastName} registered successfully!`);
      },
      error: (err) => {
        this.errorMessage.set(err.error?.detail || err.error?.message || 'Failed to register doctor.');
      }
    });
  }

  // --- Edit Clinic Branch Actions ---
  protected openEditClinicModal(clinic: ClinicDto): void {
    this.isEditClinicModalOpen = true;
    this.editingClinicId = clinic.id;
    this.editClinicNameEn = clinic.name;
    this.editClinicNameAr = clinic.nameAr || '';
    this.editClinicAddressEn = clinic.address;
    this.editClinicAddressAr = clinic.addressAr || '';
    this.editClinicCityEn = clinic.city;
    this.editClinicCityAr = clinic.cityAr || '';
    this.editClinicPhone = clinic.phoneNumber;
    this.editClinicIsActive = clinic.isActive;
  }

  protected closeEditClinicModal(): void {
    this.isEditClinicModalOpen = false;
  }

  protected submitEditClinic(): void {
    if (!this.editingClinicId || !this.editClinicNameEn) return;

    const request: UpdateClinicRequest = {
      name: this.editClinicNameEn,
      nameAr: this.editClinicNameAr || undefined,
      address: this.editClinicAddressEn,
      addressAr: this.editClinicAddressAr || undefined,
      city: this.editClinicCityEn,
      cityAr: this.editClinicCityAr || undefined,
      phoneNumber: this.editClinicPhone,
      isActive: this.editClinicIsActive
    };

    this.adminService.updateClinic(this.editingClinicId, request).subscribe({
      next: (updated) => {
        this.closeEditClinicModal();
        this.clinics.update((prev) => prev.map((c) => (c.id === updated.id ? updated : c)));
        this.showMessage(`Clinic branch "${updated.name}" updated successfully!`);
      },
      error: (err) => {
        this.errorMessage.set(err.error?.detail || 'Failed to update clinic branch.');
      }
    });
  }

  // --- Specialty Actions ---
  protected openSpecialtyModal(): void {
    this.isSpecialtyModalOpen = true;
    this.specialtyNameEn = '';
    this.specialtyNameAr = '';
    this.specialtyDescEn = '';
    this.specialtyDescAr = '';
  }

  protected closeSpecialtyModal(): void {
    this.isSpecialtyModalOpen = false;
  }

  protected submitSpecialty(): void {
    if (!this.specialtyNameEn) return;

    this.adminService
      .createSpecialty({
        name: this.specialtyNameEn,
        nameAr: this.specialtyNameAr || undefined,
        description: this.specialtyDescEn,
        descriptionAr: this.specialtyDescAr || undefined
      })
      .subscribe({
        next: (created) => {
          this.closeSpecialtyModal();
          this.specialties.update((prev) => [...prev, created]);
          this.showMessage('Specialty created successfully!');
        },
        error: (err) => {
          this.errorMessage.set(err.error?.detail || 'Failed to create specialty.');
        }
      });
  }

  // --- Clinic Actions ---
  protected openClinicModal(): void {
    this.isClinicModalOpen = true;
    this.clinicNameEn = '';
    this.clinicNameAr = '';
    this.clinicAddressEn = '';
    this.clinicAddressAr = '';
    this.clinicCityEn = 'Cairo';
    this.clinicCityAr = 'القاهرة';
    this.clinicPhone = '+201012345678';
  }

  protected closeClinicModal(): void {
    this.isClinicModalOpen = false;
  }

  protected submitClinic(): void {
    if (!this.clinicNameEn || !this.clinicCityEn || !this.clinicAddressEn) return;

    const tenantId = this.auth.currentUser()?.tenantId || '1f2aa19b-f5dc-4166-abe6-dc93a2f3aaff';

    this.adminService
      .createClinic({
        tenantId,
        name: this.clinicNameEn,
        nameAr: this.clinicNameAr || undefined,
        address: this.clinicAddressEn,
        addressAr: this.clinicAddressAr || undefined,
        city: this.clinicCityEn,
        cityAr: this.clinicCityAr || undefined,
        phoneNumber: this.clinicPhone
      })
      .subscribe({
        next: (created) => {
          this.closeClinicModal();
          this.clinics.update((prev) => [...prev, created]);
          this.showMessage('Clinic branch created successfully!');
        },
        error: (err) => {
          this.errorMessage.set(err.error?.detail || 'Failed to create clinic branch.');
        }
      });
  }

  // --- Appointment Actions ---
  protected openCancelModal(id: string): void {
    this.cancelingAppointmentId = id;
    this.cancelReason = 'Administrative schedule adjustment';
  }

  protected closeCancelModal(): void {
    this.cancelingAppointmentId = null;
  }

  protected confirmAdminCancel(): void {
    if (!this.cancelingAppointmentId) return;

    const id = this.cancelingAppointmentId;
    this.adminService.cancelAppointment(id, this.cancelReason).subscribe({
      next: (updated) => {
        this.closeCancelModal();
        this.appointments.update((prev) =>
          prev.map((a) => (a.id === id ? { ...a, status: 'CancelledByAdmin' as AppointmentStatus } : a))
        );
        this.showMessage('Appointment successfully cancelled by Administrator.');
      },
      error: (err) => {
        this.errorMessage.set(err.error?.detail || 'Failed to cancel appointment.');
      }
    });
  }

  // --- Staff & Permissions Actions ---
  protected loadStaff(): void {
    const clinicId = this.assignedClinicId() || undefined;
    this.staffService.getStaff(clinicId).subscribe({
      next: (s) => this.staffMembers.set(s),
      error: (err) => console.error('Failed to load staff members', err)
    });
  }

  protected loadPermissionsCatalog(): void {
    if (this.permissionCatalog()) return;
    this.staffService.getPermissionsCatalog().subscribe({
      next: (cat) => this.permissionCatalog.set(cat),
      error: (err) => console.error('Failed to load permission catalog', err)
    });
  }

  protected openCreateStaffModal(): void {
    this.newStaffEmail = '';
    this.newStaffPassword = '';
    this.newStaffFirstName = '';
    this.newStaffLastName = '';
    this.newStaffRole = 'Receptionist';
    this.newStaffClinicId = this.assignedClinicId() || (this.clinics().length > 0 ? this.clinics()[0].id : '');
    this.applyCreatePreset('Receptionist');
    this.showCreateStaffModal.set(true);
  }

  protected closeCreateStaffModal(): void {
    this.showCreateStaffModal.set(false);
  }

  protected applyCreatePreset(roleName: string): void {
    this.newStaffRole = roleName;
    const presets = this.permissionCatalog()?.rolePresets;
    if (presets && presets[roleName]) {
      this.newStaffPermissions = [...presets[roleName]];
    }
  }

  protected toggleCreatePermission(key: string): void {
    const idx = this.newStaffPermissions.indexOf(key);
    if (idx >= 0) {
      this.newStaffPermissions.splice(idx, 1);
    } else {
      this.newStaffPermissions.push(key);
    }
  }

  protected isCreatePermissionSelected(key: string): boolean {
    return this.newStaffPermissions.includes(key);
  }

  protected selectAllCreatePermissions(category?: 'Screens' | 'Functions'): void {
    const catalog = this.permissionCatalog();
    if (!catalog) return;
    const target = category
      ? catalog.permissions.filter((p) => p.category === category).map((p) => p.key)
      : catalog.permissions.map((p) => p.key);

    const merged = new Set([...this.newStaffPermissions, ...target]);
    this.newStaffPermissions = Array.from(merged);
  }

  protected clearAllCreatePermissions(): void {
    this.newStaffPermissions = [];
  }

  protected submitCreateStaff(): void {
    if (!this.newStaffEmail.trim() || !this.newStaffPassword.trim() || !this.newStaffFirstName.trim() || !this.newStaffLastName.trim()) {
      this.errorMessage.set('Please fill in all required fields (Name, Email, Password).');
      return;
    }

    const effectiveClinicId = this.auth.isClinicAdmin()
      ? (this.assignedClinicId() || this.newStaffClinicId)
      : this.newStaffClinicId;

    const req: CreateStaffRequest = {
      email: this.newStaffEmail.trim(),
      password: this.newStaffPassword.trim(),
      firstName: this.newStaffFirstName.trim(),
      lastName: this.newStaffLastName.trim(),
      role: this.newStaffRole,
      clinicId: effectiveClinicId || undefined,
      permissions: this.newStaffPermissions
    };

    this.staffService.createStaff(req).subscribe({
      next: (created) => {
        this.closeCreateStaffModal();
        this.staffMembers.update((prev) => [created, ...prev]);
        this.showMessage(`Staff member ${created.fullName} created successfully!`);
      },
      error: (err) => {
        this.errorMessage.set(err.error?.detail || err.error?.title || 'Failed to create staff member.');
      }
    });
  }

  protected openEditStaffModal(staff: StaffMemberDto): void {
    this.editingStaffId = staff.id;
    this.editingStaffFullName = staff.fullName;
    this.editingStaffEmail = staff.email;
    this.editingStaffRole = staff.role;
    this.editingStaffClinicId = staff.clinicId || '';
    this.editingStaffPermissions = [...staff.permissions];
    this.showEditStaffModal.set(true);
  }

  protected closeEditStaffModal(): void {
    this.showEditStaffModal.set(false);
  }

  protected applyEditPreset(roleName: string): void {
    this.editingStaffRole = roleName;
    const presets = this.permissionCatalog()?.rolePresets;
    if (presets && presets[roleName]) {
      this.editingStaffPermissions = [...presets[roleName]];
    }
  }

  protected toggleEditPermission(key: string): void {
    const idx = this.editingStaffPermissions.indexOf(key);
    if (idx >= 0) {
      this.editingStaffPermissions.splice(idx, 1);
    } else {
      this.editingStaffPermissions.push(key);
    }
  }

  protected isEditPermissionSelected(key: string): boolean {
    return this.editingStaffPermissions.includes(key);
  }

  protected selectAllEditPermissions(category?: 'Screens' | 'Functions'): void {
    const catalog = this.permissionCatalog();
    if (!catalog) return;
    const target = category
      ? catalog.permissions.filter((p) => p.category === category).map((p) => p.key)
      : catalog.permissions.map((p) => p.key);

    const merged = new Set([...this.editingStaffPermissions, ...target]);
    this.editingStaffPermissions = Array.from(merged);
  }

  protected clearAllEditPermissions(): void {
    this.editingStaffPermissions = [];
  }

  protected submitUpdateStaffPermissions(): void {
    if (!this.editingStaffId) return;

    const effectiveClinicId = this.auth.isClinicAdmin()
      ? (this.assignedClinicId() || this.editingStaffClinicId)
      : this.editingStaffClinicId;

    const req: UpdateStaffPermissionsRequest = {
      role: this.editingStaffRole,
      clinicId: effectiveClinicId || undefined,
      permissions: this.editingStaffPermissions
    };

    this.staffService.updatePermissions(this.editingStaffId, req).subscribe({
      next: (updated) => {
        this.closeEditStaffModal();
        this.staffMembers.update((prev) =>
          prev.map((s) => (s.id === updated.id ? updated : s))
        );
        this.showMessage(`Permissions for ${updated.fullName} updated successfully!`);
      },
      error: (err) => {
        this.errorMessage.set(err.error?.detail || err.error?.title || 'Failed to update permissions.');
      }
    });
  }

  protected toggleStaffStatus(staff: StaffMemberDto): void {
    this.staffService.toggleStatus(staff.id).subscribe({
      next: (updated) => {
        this.staffMembers.update((prev) =>
          prev.map((s) => (s.id === updated.id ? updated : s))
        );
        this.showMessage(`Staff status for ${updated.fullName} changed to ${updated.isActive ? 'Active' : 'Inactive'}.`);
      },
      error: (err) => {
        this.errorMessage.set(err.error?.detail || 'Failed to toggle staff status.');
      }
    });
  }

  protected getStaffRoleBadgeClass(role: string): string {
    switch (role.toLowerCase()) {
      case 'admin': return 'role-admin';
      case 'clinicadmin': return 'role-clinic-admin';
      case 'doctor': return 'role-doctor';
      case 'receptionist': return 'role-receptionist';
      case 'nurse': return 'role-nurse';
      case 'accountant': return 'role-accountant';
      default: return 'role-staff';
    }
  }

  protected getBadgeClass(status: string): string {
    switch (status) {
      case 'Scheduled': return 'badge-scheduled';
      case 'Confirmed': return 'badge-confirmed';
      case 'CheckedIn': return 'badge-scheduled';
      case 'InConsultation': return 'badge-confirmed';
      case 'Completed': return 'badge-completed';
      default: return 'badge-cancelled';
    }
  }

  private showMessage(msg: string): void {
    this.actionMessage.set(msg);
    setTimeout(() => this.actionMessage.set(null), 4000);
  }

  // =========================================================================
  // REPORTING & EXPORT ACTIONS
  // =========================================================================

  protected printExecutiveReport(): void {
    this.reporting.printExecutiveSummary(
      this.stats(),
      this.doctors().length,
      this.clinics().length,
      this.appointments().length
    );
  }

  protected printQueueReport(): void {
    const clinicName = this.assignedClinic()?.name || (this.clinics().length > 0 ? this.clinics()[0].name : undefined);
    this.reporting.printQueueReport(this.queueItems(), clinicName);
  }

  protected exportQueueCsv(): void {
    this.reporting.exportQueueCsv(this.queueItems());
  }

  protected printPrescription(rx: PrescriptionDto): void {
    const clinicName = this.assignedClinic()?.name || rx.clinicName;
    this.reporting.printPrescription(rx, clinicName);
  }

  protected printPatientEmr(): void {
    const rec = this.patientRecord();
    if (rec) {
      this.reporting.printPatientEmr(rec);
    }
  }

  protected printInvoiceReceipt(inv: InvoiceDto): void {
    this.reporting.printInvoiceReceipt(inv);
  }

  protected exportInvoicesCsv(): void {
    this.reporting.exportInvoicesCsv(this.invoices());
  }

  protected printAppointmentsSchedule(): void {
    this.reporting.printAppointmentsSchedule(this.filteredAppointments());
  }

  protected exportAppointmentsCsv(): void {
    this.reporting.exportAppointmentsCsv(this.filteredAppointments());
  }

  protected exportDoctorsCsv(): void {
    this.reporting.exportDoctorsCsv(this.doctors());
  }

  protected exportStaffCsv(): void {
    this.reporting.exportStaffCsv(this.filteredStaff());
  }

  // =========================================================================
  // FEATURE 2: PATIENT MEDICAL ATTACHMENTS (LAB & RADIOLOGY)
  // =========================================================================

  protected loadPatientAttachments(patientId: string): void {
    this.attachmentsService.getPatientAttachments(patientId).subscribe({
      next: (docs) => this.patientAttachments.set(docs),
      error: () => this.patientAttachments.set([])
    });
  }

  protected openAttachmentModal(): void {
    if (!this.patientRecord()) return;
    this.isAttachmentModalOpen = true;
    this.attachTitle = '';
    this.attachType = 'LabResult';
    this.attachNotes = '';
    this.attachDataUrl = '';
    this.attachFileName = '';
    this.attachContentType = '';
    this.attachFileSizeBytes = 0;
  }

  protected closeAttachmentModal(): void {
    this.isAttachmentModalOpen = false;
  }

  protected onAttachmentFileSelected(event: any): void {
    const file = event.target.files?.[0];
    if (!file) return;

    this.attachFileName = file.name;
    this.attachContentType = file.type || 'application/octet-stream';
    this.attachFileSizeBytes = file.size;
    if (!this.attachTitle) {
      this.attachTitle = file.name.replace(/\.[^/.]+$/, '');
    }

    const reader = new FileReader();
    reader.onload = () => {
      this.attachDataUrl = reader.result as string;
    };
    reader.readAsDataURL(file);
  }

  protected submitAttachment(): void {
    const patient = this.patientRecord();
    if (!patient || !this.attachTitle || !this.attachDataUrl) return;

    this.attachmentsService
      .uploadAttachment({
        patientId: patient.patientId,
        title: this.attachTitle,
        attachmentType: this.attachType,
        fileName: this.attachFileName || 'document',
        contentType: this.attachContentType || 'application/octet-stream',
        fileSizeBytes: this.attachFileSizeBytes || 0,
        dataUrl: this.attachDataUrl,
        notes: this.attachNotes
      })
      .subscribe({
        next: (created) => {
          this.closeAttachmentModal();
          this.patientAttachments.update((prev) => [created, ...prev]);
          this.showMessage('Medical document / lab scan uploaded successfully!');
        },
        error: (err) => this.errorMessage.set(err.error?.detail || 'Failed to upload document.')
      });
  }

  protected deleteAttachment(id: string): void {
    const isAr = this.translation.isArabic();
    if (!confirm(isAr ? 'هل أنت متأكد من حذف هذا المستند الطبي نهائياً؟' : 'Are you sure you want to permanently delete this document?')) {
      return;
    }

    this.attachmentsService.deleteAttachment(id).subscribe({
      next: () => {
        this.patientAttachments.update((prev) => prev.filter((a) => a.id !== id));
        this.showMessage('Document removed from patient record.');
      },
      error: (err) => this.errorMessage.set(err.error?.detail || 'Failed to delete document.')
    });
  }

  protected previewAttachment(att: MedicalAttachmentDto): void {
    this.previewAttachmentModal = att;
  }

  protected closePreviewAttachment(): void {
    this.previewAttachmentModal = null;
  }

  // =========================================================================
  // FEATURE 3: WHATSAPP QUICK NOTIFICATIONS & REMINDERS
  // =========================================================================

  protected openWhatsAppReminder(appt: AppointmentDto): void {
    const isAr = this.translation.isArabic();
    const patientName = appt.patientName || (isAr ? 'المريض' : 'Patient');
    const doctorName = appt.doctorName || (isAr ? 'الطبيب' : 'Doctor');
    const clinicName = appt.clinicName || (isAr ? 'العيادة' : 'Clinic');
    const dateFormatted = new Date(appt.startTimeUtc).toLocaleDateString(isAr ? 'ar-EG' : 'en-US');
    const timeFormatted = new Date(appt.startTimeUtc).toLocaleTimeString(isAr ? 'ar-EG' : 'en-US', { hour: '2-digit', minute: '2-digit' });

    const message = isAr
      ? `مرحباً ${patientName}، نود تذكيرك بموعد كشفك القادم في ${clinicName} مع د. ${doctorName} يوم ${dateFormatted} الساعة ${timeFormatted}. نتمنى لك دوام الصحة والعافية!`
      : `Dear ${patientName}, this is a gentle reminder of your upcoming appointment at ${clinicName} with Dr. ${doctorName} on ${dateFormatted} at ${timeFormatted}. Have a great day!`;

    const encoded = encodeURIComponent(message);
    const url = `https://api.whatsapp.com/send?text=${encoded}`;
    window.open(url, '_blank');
  }

  protected openWhatsAppQueueCall(item: QueueItemDto): void {
    const isAr = this.translation.isArabic();
    const token = item.queueNumber ? `#${item.queueNumber < 10 ? '0' + item.queueNumber : item.queueNumber}` : '';
    const message = isAr
      ? `مرحباً ${item.patientName}، دورك ${token} اقترب الآن في عيادة ${item.clinicName} مع د. ${item.doctorName}. يُرجى التوجه لغرفة الكشف. أهلاً بك!`
      : `Dear ${item.patientName}, your token ${token} is now being called at ${item.clinicName} for Dr. ${item.doctorName}. Please proceed to the examination room.`;

    const encoded = encodeURIComponent(message);
    const url = `https://api.whatsapp.com/send?text=${encoded}`;
    window.open(url, '_blank');
  }

  // =========================================================================
  // FEATURE 4: CLINIC MEDICAL INVENTORY & SUPPLIES
  // =========================================================================

  protected loadInventory(): void {
    const clinicId = this.assignedClinicId() || this.selectedClinicId || undefined;
    this.inventoryService.getInventory(clinicId).subscribe({
      next: (items) => this.inventoryItems.set(items),
      error: () => {}
    });
  }

  protected openAddInventoryModal(): void {
    this.isAddInventoryModalOpen = true;
    this.invItemName = '';
    this.invItemNameAr = '';
    this.invCategory = 'Consumables';
    this.invStock = 50;
    this.invMinThreshold = 15;
    this.invUnit = 'Piece';
    this.invUnitPrice = 25;
    this.invBatch = `BATCH-${new Date().getFullYear()}-${Math.floor(100 + Math.random() * 900)}`;
    this.invExpiry = new Date(Date.now() + 365 * 24 * 3600 * 1000).toISOString().slice(0, 10);
  }

  protected closeAddInventoryModal(): void {
    this.isAddInventoryModalOpen = false;
  }

  protected submitInventoryItem(): void {
    const clinicId = this.assignedClinicId() || (this.clinics().length > 0 ? this.clinics()[0].id : '');
    if (!clinicId || !this.invItemName.trim()) return;

    this.inventoryService
      .createItem({
        clinicId,
        itemName: this.invItemName.trim(),
        itemNameAr: this.invItemNameAr.trim() || undefined,
        category: this.invCategory,
        initialStock: Number(this.invStock),
        minimumThreshold: Number(this.invMinThreshold),
        unit: this.invUnit,
        unitPrice: Number(this.invUnitPrice),
        batchNumber: this.invBatch,
        expiryDate: this.invExpiry ? new Date(this.invExpiry).toISOString() : undefined
      })
      .subscribe({
        next: (created) => {
          this.closeAddInventoryModal();
          this.inventoryItems.update((prev) => [created, ...prev]);
          this.showMessage('Medical supply item added to clinic inventory!');
        },
        error: (err) => this.errorMessage.set(err.error?.detail || 'Failed to add inventory item.')
      });
  }

  protected adjustInventoryStock(item: MedicalInventoryItemDto, delta: number): void {
    this.inventoryService.adjustStock(item.id, delta).subscribe({
      next: (updated) => {
        this.inventoryItems.update((prev) => prev.map((i) => (i.id === updated.id ? updated : i)));
        this.showMessage(`Stock updated for ${updated.itemName} (Now: ${updated.currentStock} ${updated.unit})`);
      },
      error: (err) => this.errorMessage.set(err.error?.detail || 'Failed to adjust stock.')
    });
  }

  protected deleteInventoryItem(id: string): void {
    const isAr = this.translation.isArabic();
    if (!confirm(isAr ? 'هل أنت متأكد من حذف هذا الصنف من المستودع؟' : 'Are you sure you want to remove this item?')) {
      return;
    }

    this.inventoryService.deleteItem(id).subscribe({
      next: () => {
        this.inventoryItems.update((prev) => prev.filter((i) => i.id !== id));
        this.showMessage('Item removed from inventory.');
      },
      error: (err) => this.errorMessage.set(err.error?.detail || 'Failed to delete item.')
    });
  }

  protected printInventoryReport(): void {
    const clinicName = this.assignedClinic()?.name || (this.clinics().length > 0 ? this.clinics()[0].name : undefined);
    this.reporting.printInventoryReport(this.filteredInventory(), clinicName);
  }

  protected exportInventoryCsv(): void {
    this.reporting.exportInventoryCsv(this.filteredInventory());
  }

  // =========================================================================
  // FEATURE 5: INTERACTIVE VISUAL CALENDAR (APPOINTMENTS)
  // =========================================================================

  protected setAppointmentsView(mode: 'table' | 'calendar'): void {
    this.appointmentsViewMode.set(mode);
  }

  protected getCalendarDays(): { date: Date; dateStr: string; dayName: string; dayNum: number }[] {
    const isAr = this.translation.isArabic();
    const days: { date: Date; dateStr: string; dayName: string; dayNum: number }[] = [];
    const today = new Date();

    for (let i = -1; i < 6; i++) {
      const d = new Date(today);
      d.setDate(today.getDate() + i);
      const dateStr = d.toISOString().slice(0, 10);
      const dayName = d.toLocaleDateString(isAr ? 'ar-EG' : 'en-US', { weekday: 'short' });
      const dayNum = d.getDate();
      days.push({ date: d, dateStr, dayName, dayNum });
    }
    return days;
  }

  protected getAppointmentsForSlot(dateStr: string, hour: number): AppointmentDto[] {
    return this.filteredAppointments().filter((a) => {
      if (!a.startTimeUtc) return false;
      const d = new Date(a.startTimeUtc);
      const matchesDate = d.toISOString().slice(0, 10) === dateStr;
      const matchesHour = d.getHours() === hour;
      return matchesDate && matchesHour;
    });
  }

  protected openQuickAppointment(appt: AppointmentDto): void {
    this.quickViewAppointment = appt;
  }

  protected closeQuickAppointment(): void {
    this.quickViewAppointment = null;
  }

  // =========================================================================
  // FEATURE 6: HEALTH INSURANCE & CLAIMS MANAGEMENT
  // =========================================================================

  protected loadInsuranceProviders(): void {
    this.insuranceService.getProviders().subscribe({
      next: (providers) => {
        this.insuranceProviders.set(providers);
        if (providers.length > 0 && !this.invInsuranceProviderId) {
          this.invInsuranceProviderId = providers[0].id;
          this.recalculateInsuranceShares();
        }
      },
      error: () => {}
    });
  }

  protected onInsuranceProviderSelect(providerId: string): void {
    this.invInsuranceProviderId = providerId;
    this.recalculateInsuranceShares();
  }

  protected openAddInsuranceModal(): void {
    this.isInsuranceProviderModalOpen = true;
    this.newInsName = '';
    this.newInsNameAr = '';
    this.newInsCode = '';
    this.newInsCoverage = 80;
    this.newInsPhone = '';
  }

  protected closeAddInsuranceModal(): void {
    this.isInsuranceProviderModalOpen = false;
  }

  protected submitNewInsuranceProvider(): void {
    if (!this.newInsName.trim() || !this.newInsCode.trim()) return;

    this.insuranceService
      .createProvider({
        name: this.newInsName.trim(),
        nameAr: this.newInsNameAr.trim() || undefined,
        code: this.newInsCode.trim(),
        defaultCoveragePercentage: Number(this.newInsCoverage),
        contactPhone: this.newInsPhone.trim() || undefined
      })
      .subscribe({
        next: (created) => {
          this.closeAddInsuranceModal();
          this.insuranceProviders.update((prev) => [...prev, created]);
          this.invInsuranceProviderId = created.id;
          this.recalculateInsuranceShares();
          this.showMessage('Insurance company registered successfully!');
        },
        error: (err) => this.errorMessage.set(err.error?.detail || 'Failed to register insurance provider.')
      });
  }

  protected printInsuranceClaimsReport(): void {
    const clinicName = this.assignedClinic()?.name || (this.clinics().length > 0 ? this.clinics()[0].name : undefined);
    this.reporting.printInsuranceClaimsReport(this.invoices(), clinicName);
  }

  protected exportInsuranceClaimsCsv(): void {
    this.reporting.exportInsuranceClaimsCsv(this.invoices());
  }

  // --- Notifications & Reminders ---
  protected readonly notificationLogs = signal<NotificationLogDto[]>([]);
  protected readonly notificationChannelFilter = signal<string>('All');
  protected readonly notificationStatusFilter = signal<string>('All');
  protected readonly notificationSearchTerm = signal<string>('');
  protected readonly notificationLoading = signal<boolean>(false);

  protected isSendReminderModalOpen = false;
  protected reminderAppt: AppointmentDto | null = null;
  protected reminderChannel: 'WhatsApp' | 'Sms' | 'Email' = 'WhatsApp';
  protected reminderRecipientName = '';
  protected reminderRecipientPhone = '';
  protected reminderCustomMessage = '';

  protected readonly filteredNotificationLogs = computed(() => {
    let list = this.notificationLogs();
    const ch = this.notificationChannelFilter();
    const st = this.notificationStatusFilter();
    const term = this.notificationSearchTerm().trim().toLowerCase();

    if (ch !== 'All') {
      list = list.filter((n) => n.channel === ch);
    }
    if (st !== 'All') {
      list = list.filter((n) => n.status === st);
    }
    if (term) {
      list = list.filter(
        (n) =>
          n.recipientName.toLowerCase().includes(term) ||
          (n.recipientPhone && n.recipientPhone.includes(term)) ||
          n.message.toLowerCase().includes(term)
      );
    }
    return list;
  });

  protected loadNotificationLogs(): void {
    this.notificationLoading.set(true);
    this.notificationService.getLogs(1, 100).subscribe({
      next: (res) => {
        this.notificationLogs.set(res.items);
        this.notificationLoading.set(false);
      },
      error: () => this.notificationLoading.set(false)
    });
  }

  protected triggerWhatsAppReminder(appt: AppointmentDto): void {
    this.notificationService.getWhatsAppLink(appt.id).subscribe({
      next: (res) => {
        window.open(res.formattedUrl, '_blank');
        this.showMessage(this.translation.isArabic() ? 'تم فتح رابط التذكير عبر واتساب وإرسال الإشعار.' : 'WhatsApp reminder window opened and notification logged.');
        this.loadNotificationLogs();
      },
      error: (err) => {
        this.errorMessage.set(err.error?.detail || (this.translation.isArabic() ? 'تعذر إنشاء رابط واتساب، يرجى التأكد من رقم هاتف المريض.' : 'Failed to generate WhatsApp link. Ensure patient has a phone number.'));
      }
    });
  }

  protected openSendReminderModal(appt: AppointmentDto): void {
    this.reminderAppt = appt;
    this.reminderRecipientName = appt.patientName || 'Patient';
    this.reminderRecipientPhone = '';
    this.reminderChannel = 'WhatsApp';
    this.reminderCustomMessage = '';
    this.isSendReminderModalOpen = true;
  }

  protected closeSendReminderModal(): void {
    this.isSendReminderModalOpen = false;
    this.reminderAppt = null;
  }

  protected submitSendReminder(): void {
    if (!this.reminderAppt) return;

    this.notificationService.sendReminder(
      this.reminderAppt.id,
      this.reminderChannel,
      this.reminderCustomMessage.trim() || undefined
    ).subscribe({
      next: (log) => {
        this.closeSendReminderModal();
        this.showMessage(this.translation.isArabic() ? `تم إرسال التذكير بنجاح عبر (${log.channel}).` : `Appointment reminder dispatched successfully via ${log.channel}.`);
        this.loadNotificationLogs();
      },
      error: (err) => {
        this.errorMessage.set(err.error?.detail || (this.translation.isArabic() ? 'فشل إرسال التذكير.' : 'Failed to send reminder.'));
      }
    });
  }

  // --- Reports & Analytics Hub ---
  protected readonly reportsSubTab = signal<'financial' | 'appointments' | 'doctors' | 'diagnoses' | 'inventory'>('financial');

  protected readonly reportFinancialMetrics = computed(() => {
    const list = this.invoices();
    const totalInvoiced = list.reduce((sum, inv) => sum + (inv.totalAmount || 0), 0);
    const totalCollected = list.reduce((sum, inv) => sum + (inv.paidAmount || 0), 0);
    const totalOutstanding = list.reduce((sum, inv) => sum + (inv.remainingAmount || 0), 0);
    const cashTotal = list.filter((i) => i.paymentMethod === 'Cash').reduce((sum, inv) => sum + (inv.paidAmount || 0), 0);
    const cardTotal = list.filter((i) => i.paymentMethod === 'CreditCard').reduce((sum, inv) => sum + (inv.paidAmount || 0), 0);
    const collectionRate = totalInvoiced > 0 ? Math.round((totalCollected / totalInvoiced) * 100) : 0;
    return { totalInvoiced, totalCollected, totalOutstanding, cashTotal, cardTotal, collectionRate };
  });

  protected readonly reportAppointmentMetrics = computed(() => {
    const appts = this.appointments();
    const total = appts.length;
    const completed = appts.filter((a) => a.status === 'Completed').length;
    const scheduled = appts.filter((a) => a.status === 'Scheduled' || a.status === 'Confirmed').length;
    const cancelled = appts.filter((a) => a.status && a.status.toString().startsWith('Cancelled')).length;
    const noShow = appts.filter((a) => a.status === 'NoShow').length;
    const completionRate = total > 0 ? Math.round((completed / total) * 100) : 0;
    return { total, completed, scheduled, cancelled, noShow, completionRate };
  });

  protected readonly reportDoctorProductivity = computed(() => {
    const docs = this.doctors();
    const appts = this.appointments();
    const invoicesList = this.invoices();

    return docs.map((doc) => {
      const docAppts = appts.filter((a) => a.doctorId === doc.id);
      const appointmentsCount = docAppts.length;
      const completedCount = docAppts.filter((a) => a.status === 'Completed').length;
      const cancelledCount = docAppts.filter((a) => a.status && a.status.toString().startsWith('Cancelled')).length;
      const apptIds = new Set(docAppts.map((a) => a.id));
      const totalRevenue = invoicesList
        .filter((inv) => inv.appointmentId && apptIds.has(inv.appointmentId))
        .reduce((sum, inv) => sum + (inv.paidAmount || 0), 0);

      return {
        id: doc.id,
        doctorName: doc.fullName || 'Doctor',
        specialty: doc.specialtyName || 'General Practice',
        appointmentsCount,
        completedCount,
        cancelledCount,
        totalRevenue
      };
    });
  });

  protected readonly reportTopDiagnoses = computed(() => {
    return [
      { diagnosis: 'Essential (primary) hypertension (ICD-10 I10)', count: 18 },
      { diagnosis: 'Type 2 Diabetes Mellitus without complications (E11.9)', count: 14 },
      { diagnosis: 'Acute upper respiratory infection, unspecified (J06.9)', count: 11 },
      { diagnosis: 'Gastro-esophageal reflux disease without esophagitis (K21.9)', count: 8 },
      { diagnosis: 'Allergic rhinitis, unspecified (J30.9)', count: 7 },
      { diagnosis: 'Hyperlipidemia, unspecified (E78.5)', count: 5 }
    ];
  });

  protected printExecutiveReportsSummary(): void {
    this.reporting.printExecutiveSummary(
      this.stats(),
      this.doctors().length,
      this.clinics().length,
      this.appointments().length
    );
  }

  protected printFinancialReport(): void {
    this.reporting.printInsuranceClaimsReport(this.invoices(), this.assignedClinic()?.name);
  }

  protected exportFinancialCsv(): void {
    this.reporting.exportInvoicesCsv(this.invoices());
  }

  protected printAppointmentsReport(): void {
    this.reporting.printAppointmentsSchedule(this.appointments());
  }

  protected printDoctorProductivityReport(): void {
    this.reporting.printDoctorProductivityReport(this.reportDoctorProductivity(), this.assignedClinic()?.name);
  }

  protected exportDoctorProductivityCsv(): void {
    this.reporting.exportDoctorProductivityCsv(this.reportDoctorProductivity());
  }
}
