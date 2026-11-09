import { Component, inject, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../../core/services/admin.service';
import { TranslatePipe } from '../../../core/pipes/translate.pipe';
import { DashboardStatsDto } from '../../../core/models/dashboard.models';
import { AppointmentDto } from '../../../core/models/appointment.models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [DatePipe, FormsModule, TranslatePipe],
  templateUrl: './dashboard.html',
  styleUrls: ['./dashboard.scss']
})
export class DashboardComponent implements OnInit {
  private readonly adminService = inject(AdminService);

  protected readonly stats = signal<DashboardStatsDto | null>(null);
  protected readonly appointments = signal<AppointmentDto[]>([]);
  protected readonly loading = signal(true);
  protected readonly successMessage = signal<string | null>(null);

  // New Specialty Modal Form
  protected isSpecialtyModalOpen = false;
  protected specialtyNameEn = '';
  protected specialtyNameAr = '';
  protected specialtyDescEn = '';
  protected specialtyDescAr = '';

  ngOnInit(): void {
    this.loadDashboard();
  }

  protected loadDashboard(): void {
    this.loading.set(true);

    this.adminService.getDashboardStats().subscribe({
      next: (data) => this.stats.set(data),
      error: () => {}
    });

    this.adminService.getAllAppointments().subscribe({
      next: (data) => {
        this.appointments.set(data);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

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
        next: () => {
          this.closeSpecialtyModal();
          this.successMessage.set('Bilingual specialty created successfully!');
          setTimeout(() => this.successMessage.set(null), 3000);
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
}
