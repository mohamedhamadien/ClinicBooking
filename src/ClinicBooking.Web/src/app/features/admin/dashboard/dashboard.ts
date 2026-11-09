import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../../core/services/admin.service';
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

    this.adminService.getDashboardStats().subscribe({
      next: (s) => this.stats.set(s),
      error: () => {}
    });

    this.adminService.getAllAppointments().subscribe({
      next: (a) => {
        this.appointments.set(a);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });

    this.doctorService.getClinics().subscribe((c) => this.clinics.set(c));
    this.doctorService.getSpecialties().subscribe((sp) => this.specialties.set(sp));
    this.doctorService.getDoctors().subscribe((d) => this.doctors.set(d));
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
