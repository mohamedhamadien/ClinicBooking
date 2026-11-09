import { Component, inject, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AppointmentService } from '../../../core/services/appointment.service';
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

  protected readonly appointments = signal<AppointmentDto[]>([]);
  protected readonly loading = signal(true);
  protected readonly actionMessage = signal<string | null>(null);
  protected readonly errorMessage = signal<string | null>(null);

  // Cancellation modal state
  protected cancelingAppointmentId: string | null = null;
  protected cancelReason = 'Personal circumstances';

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

  protected getBadgeClass(status: string): string {
    switch (status) {
      case 'Scheduled': return 'badge-scheduled';
      case 'Confirmed': return 'badge-confirmed';
      case 'Completed': return 'badge-completed';
      default: return 'badge-cancelled';
    }
  }

  protected canCancel(status: string): boolean {
    return status === 'Scheduled' || status === 'Confirmed';
  }
}
