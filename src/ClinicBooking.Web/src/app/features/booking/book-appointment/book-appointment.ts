import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { DoctorService } from '../../../core/services/doctor.service';
import { AppointmentService } from '../../../core/services/appointment.service';
import { AuthService } from '../../../core/services/auth.service';
import { TranslationService } from '../../../core/services/translation.service';
import { TranslatePipe } from '../../../core/pipes/translate.pipe';
import { AvailableSlotDto, ClinicDto, DoctorDto } from '../../../core/models/doctor.models';

@Component({
  selector: 'app-book-appointment',
  standalone: true,
  imports: [FormsModule, DatePipe, TranslatePipe],
  templateUrl: './book-appointment.html',
  styleUrls: ['./book-appointment.scss']
})
export class BookAppointmentComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly doctorService = inject(DoctorService);
  private readonly appointmentService = inject(AppointmentService);
  protected readonly auth = inject(AuthService);
  protected readonly translation = inject(TranslationService);

  protected readonly doctor = signal<DoctorDto | null>(null);
  protected readonly clinics = signal<ClinicDto[]>([]);
  protected readonly availableSlots = signal<AvailableSlotDto[]>([]);

  protected readonly loadingDoctor = signal(true);
  protected readonly loadingSlots = signal(false);
  protected readonly bookingInProgress = signal(false);

  protected readonly errorMessage = signal<string | null>(null);
  protected readonly successMessage = signal<string | null>(null);

  // Form selections
  protected selectedClinicId = '';
  protected selectedDate = '';
  protected selectedSlot: AvailableSlotDto | null = null;
  protected notes = '';

  ngOnInit(): void {
    const doctorId = this.route.snapshot.paramMap.get('id');
    if (!doctorId) {
      this.router.navigate(['/doctors']);
      return;
    }

    // Set default date to Today in YYYY-MM-DD
    const now = new Date();
    const pad = (n: number) => (n < 10 ? '0' + n : '' + n);
    this.selectedDate = `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`;

    // Load Doctor details and Clinics
    this.doctorService.getDoctorById(doctorId).subscribe({
      next: (doc) => {
        this.doctor.set(doc);
        this.loadingDoctor.set(false);

        // Pre-select first clinic if assigned
        if (doc.doctorClinics.length > 0) {
          this.selectedClinicId = doc.doctorClinics[0].clinicId;
        } else if (this.clinics().length > 0) {
          this.selectedClinicId = this.clinics()[0].id;
        }

        this.loadAvailableSlots();
      },
      error: () => {
        this.loadingDoctor.set(false);
        this.router.navigate(['/doctors']);
      }
    });

    this.doctorService.getClinics().subscribe((data) => {
      this.clinics.set(data);
      if (!this.selectedClinicId && data.length > 0) {
        this.selectedClinicId = data[0].id;
        this.loadAvailableSlots();
      }
    });
  }

  protected onDateOrClinicChange(): void {
    this.selectedSlot = null;
    this.loadAvailableSlots();
  }

  protected selectSlot(slot: AvailableSlotDto): void {
    if (!slot.isAvailable) return;
    this.selectedSlot = slot;
    this.errorMessage.set(null);
  }

  protected loadAvailableSlots(): void {
    const doc = this.doctor();
    if (!doc || !this.selectedClinicId || !this.selectedDate) return;

    this.loadingSlots.set(true);
    this.errorMessage.set(null);

    this.doctorService
      .getAvailableSlots(doc.id, this.selectedClinicId, this.selectedDate)
      .subscribe({
        next: (slots) => {
          this.availableSlots.set(slots);
          this.loadingSlots.set(false);
        },
        error: (err) => {
          this.loadingSlots.set(false);
          this.availableSlots.set([]);
          this.errorMessage.set(
            err.error?.detail || err.error?.message || 'Could not load slots.'
          );
        }
      });
  }

  protected confirmBooking(): void {
    if (!this.auth.isAuthenticated()) {
      this.router.navigate(['/auth/login']);
      return;
    }

    const doc = this.doctor();
    const user = this.auth.currentUser();

    if (!doc || !this.selectedSlot || !user) return;

    this.bookingInProgress.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    this.appointmentService
      .bookAppointment({
        tenantId: doc.tenantId,
        doctorId: doc.id,
        patientId: user.id, // Current authenticated user is the patient
        clinicId: this.selectedClinicId,
        startTimeUtc: this.selectedSlot.startTimeUtc,
        endTimeUtc: this.selectedSlot.endTimeUtc,
        notes: this.notes || undefined
      })
      .subscribe({
        next: () => {
          this.bookingInProgress.set(false);
          this.successMessage.set('Appointment successfully booked! Redirecting...');
          setTimeout(() => this.router.navigate(['/my-appointments']), 1500);
        },
        error: (err) => {
          this.bookingInProgress.set(false);
          // Handle 409 Conflict with double-booking error
          if (err.status === 409) {
            this.errorMessage.set(
              'Conflict: This appointment slot was just taken by another patient or you already have an overlapping appointment. Please choose a different slot.'
            );
            this.loadAvailableSlots(); // Refresh slot availability
          } else {
            this.errorMessage.set(
              err.error?.detail || err.error?.message || 'Booking failed. Please try again.'
            );
          }
        }
      });
  }
}
