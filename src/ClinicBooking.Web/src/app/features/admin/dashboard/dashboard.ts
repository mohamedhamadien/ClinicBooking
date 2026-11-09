import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminService, RegisterDoctorRequest, UpdateClinicRequest } from '../../../core/services/admin.service';
import { DoctorService } from '../../../core/services/doctor.service';
import { AuthService } from '../../../core/services/auth.service';
import { TranslationService } from '../../../core/services/translation.service';
import { TranslatePipe } from '../../../core/pipes/translate.pipe';
import { DashboardStatsDto } from '../../../core/models/dashboard.models';
import { AppointmentDto, AppointmentStatus } from '../../../core/models/appointment.models';
import { ClinicDto, DoctorDto, SpecialtyDto } from '../../../core/models/doctor.models';

export type AdminTab = 'overview' | 'appointments' | 'clinics' | 'specialties' | 'doctors';

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

  protected readonly loading = signal(true);
  protected readonly actionMessage = signal<string | null>(null);
  protected readonly errorMessage = signal<string | null>(null);

  // Current Clinic Scope (if Clinic Admin)
  protected readonly assignedClinicId = computed(() => this.auth.currentUser()?.clinicId || null);
  protected readonly assignedClinic = computed(() => {
    const id = this.assignedClinicId();
    return id ? this.clinics().find((c) => c.id === id) || null : null;
  });

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

  // Modal: Register New Doctor
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
  protected docInitialShiftDay = 0; // Sunday
  protected docInitialShiftStart = '09:00:00';
  protected docInitialShiftEnd = '17:00:00';

  // Modal: Edit Clinic Branch
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

  // Modal: New Specialty
  protected isSpecialtyModalOpen = false;
  protected specialtyNameEn = '';
  protected specialtyNameAr = '';
  protected specialtyDescEn = '';
  protected specialtyDescAr = '';

  // Modal: New Clinic Branch
  protected isClinicModalOpen = false;
  protected clinicNameEn = '';
  protected clinicNameAr = '';
  protected clinicAddressEn = '';
  protected clinicAddressAr = '';
  protected clinicCityEn = 'Cairo';
  protected clinicCityAr = 'القاهرة';
  protected clinicPhone = '+201012345678';

  // Modal: Cancel Appointment
  protected cancelingAppointmentId: string | null = null;
  protected cancelReason = 'Administrative schedule adjustment';

  ngOnInit(): void {
    this.loadAllData();
  }

  protected setTab(tab: AdminTab): void {
    this.activeTab.set(tab);
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

    this.doctorService.getClinics().subscribe((c) => this.clinics.set(c));
    this.doctorService.getSpecialties().subscribe((sp) => this.specialties.set(sp));
    this.doctorService.getDoctors(undefined, clinicId).subscribe((d) => this.doctors.set(d));
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
      case 'Completed': return 'badge-completed';
      default: return 'badge-cancelled';
    }
  }

  private showMessage(msg: string): void {
    this.actionMessage.set(msg);
    setTimeout(() => this.actionMessage.set(null), 3500);
  }
}
