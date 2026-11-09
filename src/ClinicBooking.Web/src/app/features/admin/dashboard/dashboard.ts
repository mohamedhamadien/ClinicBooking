import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminService, RegisterDoctorRequest, UpdateClinicRequest } from '../../../core/services/admin.service';
import { DoctorService } from '../../../core/services/doctor.service';
import { ClinicalService } from '../../../core/services/clinical.service';
import { AuthService } from '../../../core/services/auth.service';
import { TranslationService } from '../../../core/services/translation.service';
import { TranslatePipe } from '../../../core/pipes/translate.pipe';
import { DashboardStatsDto } from '../../../core/models/dashboard.models';
import { AppointmentDto, AppointmentStatus } from '../../../core/models/appointment.models';
import { ClinicDto, DoctorDto, SpecialtyDto } from '../../../core/models/doctor.models';
import {
  ConsultationDto,
  InvoiceDto,
  PatientMedicalRecordDto,
  PrescriptionDto,
  QueueItemDto
} from '../../../core/models/clinical.models';

export type AdminTab =
  | 'overview'
  | 'queue'
  | 'consultation'
  | 'records'
  | 'billing'
  | 'appointments'
  | 'doctors'
  | 'clinics'
  | 'specialties';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [DatePipe, FormsModule, TranslatePipe],
  templateUrl: './dashboard.html',
  styleUrls: ['./dashboard.scss']
})
export class DashboardComponent implements OnInit {
  private readonly adminService = inject(AdminService);
  private readonly doctorService = inject(DoctorService);
  private readonly clinicalService = inject(ClinicalService);
  protected readonly auth = inject(AuthService);
  protected readonly translation = inject(TranslationService);

  // Active Tab
  protected activeTab = signal<AdminTab>('overview');

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
  protected historyPatientId = '';

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

  ngOnInit(): void {
    this.loadAllData();
  }

  protected setTab(tab: AdminTab): void {
    this.activeTab.set(tab);
    if (tab === 'queue') {
      this.loadQueue();
    } else if (tab === 'billing') {
      this.loadInvoices();
    }
  }

  protected loadAllData(): void {
    this.loading.set(true);
    const clinicId = this.assignedClinicId() || undefined;

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
    this.doctorService.getDoctors(undefined, clinicId).subscribe((d) => this.doctors.set(d));
  }

  // --- Queue Actions ---
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
    this.invItems = [{ description: 'Specialist Consultation Fee', quantity: 1, unitPrice: 350 }];
  }

  protected closeInvoiceModal(): void {
    this.isInvoiceModalOpen = false;
  }

  protected addInvoiceItemRow(): void {
    this.invItems.push({ description: '', quantity: 1, unitPrice: 100 });
  }

  protected removeInvoiceItemRow(index: number): void {
    if (this.invItems.length > 1) {
      this.invItems.splice(index, 1);
    }
  }

  protected submitInvoice(): void {
    if (!this.invPatientId || !this.invClinicId || this.invItems.length === 0) return;

    const tenantId = this.auth.currentUser()?.tenantId || '1f2aa19b-f5dc-4166-abe6-dc93a2f3aaff';

    this.clinicalService
      .createInvoice({
        tenantId,
        patientId: this.invPatientId,
        clinicId: this.invClinicId,
        discountAmount: this.invDiscount,
        taxAmount: this.invTax,
        paymentMethod: Number(this.invMethod),
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
        this.showMessage(`Invoice #${updated.invoiceNumber} marked as Paid!`);
      },
      error: (err) => this.errorMessage.set(err.error?.detail || 'Failed to pay invoice.')
    });
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
}
