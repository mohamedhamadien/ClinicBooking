import { Component, inject, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AppointmentService } from '../../../core/services/appointment.service';
import { TranslationService } from '../../../core/services/translation.service';
import { ReportingService } from '../../../core/services/reporting.service';
import { TranslatePipe } from '../../../core/pipes/translate.pipe';
import { AppointmentDto } from '../../../core/models/appointment.models';

@Component({
  selector: 'app-my-appointments',
  standalone: true,
  imports: [DatePipe, FormsModule, RouterLink, TranslatePipe],
  templateUrl: './my-appointments.html',
  styleUrls: ['./my-appointments.scss']
})
export class MyAppointmentsComponent implements OnInit {
  private readonly appointmentService = inject(AppointmentService);
  protected readonly translation = inject(TranslationService);
  protected readonly reporting = inject(ReportingService);

  protected readonly appointments = signal<AppointmentDto[]>([]);
  protected readonly loading = signal(true);
  protected readonly actionMessage = signal<string | null>(null);
  protected readonly errorMessage = signal<string | null>(null);

  // Cancellation modal state
  protected cancelingAppointmentId: string | null = null;
  protected cancelReason = 'Personal circumstances';

  // Rescheduling modal state
  protected reschedulingAppointment: AppointmentDto | null = null;
  protected rescheduleDate = '';
  protected rescheduleTime = '10:00';
  protected rescheduleDurationMinutes = 30;

  ngOnInit(): void {
    this.loadAppointments();
  }

  protected loadAppointments(): void {
    this.loading.set(true);
    this.appointmentService.getMyAppointments().subscribe({
      next: (data) => {
        this.appointments.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(err.error?.detail || 'Failed to load your appointments.');
      }
    });
  }

  protected openCancelModal(id: string): void {
    this.cancelingAppointmentId = id;
    this.cancelReason = 'Personal circumstances';
  }

  protected closeCancelModal(): void {
    this.cancelingAppointmentId = null;
  }

  protected confirmCancel(): void {
    if (!this.cancelingAppointmentId) return;

    const id = this.cancelingAppointmentId;
    this.appointmentService.cancelAppointment(id, this.cancelReason).subscribe({
      next: () => {
        this.closeCancelModal();
        this.actionMessage.set('Appointment cancelled successfully.');
        this.loadAppointments();
      },
      error: (err) => {
        this.errorMessage.set(err.error?.detail || 'Failed to cancel appointment.');
      }
    });
  }

  protected openRescheduleModal(appt: AppointmentDto): void {
    this.reschedulingAppointment = appt;
    const start = new Date(appt.startTimeUtc);
    this.rescheduleDate = start.toISOString().split('T')[0];
    const hours = String(start.getUTCHours()).padStart(2, '0');
    const mins = String(start.getUTCMinutes()).padStart(2, '0');
    this.rescheduleTime = `${hours}:${mins}`;
    this.rescheduleDurationMinutes = 30;
  }

  protected closeRescheduleModal(): void {
    this.reschedulingAppointment = null;
  }

  protected confirmReschedule(): void {
    if (!this.reschedulingAppointment || !this.rescheduleDate || !this.rescheduleTime) return;

    const [hours, minutes] = this.rescheduleTime.split(':').map(Number);
    const [year, month, day] = this.rescheduleDate.split('-').map(Number);
    const startUtc = new Date(Date.UTC(year, month - 1, day, hours, minutes));
    const endUtc = new Date(startUtc.getTime() + this.rescheduleDurationMinutes * 60000);

    this.appointmentService.rescheduleAppointment(
      this.reschedulingAppointment.id,
      startUtc.toISOString(),
      endUtc.toISOString()
    ).subscribe({
      next: () => {
        this.closeRescheduleModal();
        this.actionMessage.set(this.translation.isArabic() ? 'تمت إعادة جدولة الموعد بنجاح.' : 'Appointment rescheduled successfully.');
        this.loadAppointments();
      },
      error: (err) => {
        this.errorMessage.set(err.error?.detail || (this.translation.isArabic() ? 'فشلت إعادة جدولة الموعد.' : 'Failed to reschedule appointment.'));
      }
    });
  }

  protected getBadgeClass(status: string | number): string {
    const s = String(status);
    if (s === 'Scheduled' || s === '1' || s === 'PendingApproval' || s === '2') return 'badge-scheduled';
    if (s === 'Confirmed' || s === '3') return 'badge-confirmed';
    if (s === 'Completed' || s === '4') return 'badge-completed';
    return 'badge-cancelled';
  }

  protected canCancel(status: string | number): boolean {
    const s = String(status);
    return s === 'Scheduled' || s === '1' || s === 'Confirmed' || s === '3';
  }

  protected printSlip(appt: AppointmentDto): void {
    this.reporting.printPatientAppointmentSlip(appt);
  }
}
