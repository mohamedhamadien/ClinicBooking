import { Component, inject, OnInit, OnDestroy, signal, computed } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { ClinicalService } from '../../../core/services/clinical.service';
import { DoctorService } from '../../../core/services/doctor.service';
import { TranslationService } from '../../../core/services/translation.service';
import { QueueItemDto } from '../../../core/models/clinical.models';
import { ClinicDto } from '../../../core/models/doctor.models';

@Component({
  selector: 'app-queue-tv-display',
  standalone: true,
  imports: [CommonModule, DatePipe, FormsModule, RouterLink],
  templateUrl: './queue-tv-display.html',
  styleUrls: ['./queue-tv-display.scss']
})
export class QueueTvDisplayComponent implements OnInit, OnDestroy {
  private readonly clinicalService = inject(ClinicalService);
  private readonly doctorService = inject(DoctorService);
  private readonly route = inject(ActivatedRoute);
  protected readonly translation = inject(TranslationService);

  protected readonly clinics = signal<ClinicDto[]>([]);
  protected selectedClinicId = '';
  protected readonly queueItems = signal<QueueItemDto[]>([]);
  protected readonly isAudioEnabled = signal<boolean>(true);
  protected readonly isFullscreen = signal<boolean>(false);
  protected readonly currentTime = signal<Date>(new Date());

  private pollInterval: any;
  private clockInterval: any;
  private lastCalledToken: number | null = null;
  private audioCtx: AudioContext | null = null;

  // Active called patient (InConsultation status or first CheckedIn)
  protected readonly currentCall = computed(() => {
    const inExam = this.queueItems().find((q) => q.status === 'InConsultation');
    if (inExam) return inExam;
    const checked = this.queueItems().find((q) => q.status === 'CheckedIn' && q.queueNumber);
    return checked || null;
  });

  // Upcoming waiting queue
  protected readonly upcomingQueue = computed(() => {
    return this.queueItems().filter((q) => q.status === 'CheckedIn' && q.appointmentId !== this.currentCall()?.appointmentId);
  });

  ngOnInit(): void {
    this.loadClinics();
    this.clockInterval = setInterval(() => {
      this.currentTime.set(new Date());
    }, 1000);

    // Poll queue every 6 seconds
    this.pollInterval = setInterval(() => {
      this.refreshQueue();
    }, 6000);
  }

  ngOnDestroy(): void {
    if (this.pollInterval) clearInterval(this.pollInterval);
    if (this.clockInterval) clearInterval(this.clockInterval);
    if (this.audioCtx) this.audioCtx.close();
  }

  protected loadClinics(): void {
    const qClinicId = this.route.snapshot.queryParamMap.get('clinicId');
    this.doctorService.getClinics().subscribe({
      next: (clinics: ClinicDto[]) => {
        this.clinics.set(clinics);
        if (qClinicId && clinics.some((c) => c.id === qClinicId)) {
          this.selectedClinicId = qClinicId;
        } else if (clinics.length > 0) {
          this.selectedClinicId = clinics[0].id;
        }
        if (this.selectedClinicId) {
          this.refreshQueue();
        }
      },
      error: () => {}
    });
  }

  protected onClinicChange(): void {
    this.refreshQueue();
  }

  protected refreshQueue(): void {
    if (!this.selectedClinicId) return;
    this.clinicalService.getClinicQueue(this.selectedClinicId).subscribe({
      next: (items: QueueItemDto[]) => {
        this.queueItems.set(items);
        this.checkAndPlayChime();
      },
      error: () => {}
    });
  }

  private checkAndPlayChime(): void {
    const current = this.currentCall();
    if (current && current.queueNumber && current.queueNumber !== this.lastCalledToken) {
      this.lastCalledToken = current.queueNumber;
      if (this.isAudioEnabled()) {
        this.playPleasantChime();
      }
    }
  }

  protected playPleasantChime(): void {
    try {
      if (!this.audioCtx) {
        const AudioContextClass = window.AudioContext || (window as any).webkitAudioContext;
        this.audioCtx = new AudioContextClass();
      }
      if (this.audioCtx.state === 'suspended') {
        this.audioCtx.resume();
      }

      const now = this.audioCtx.currentTime;

      // Note 1: E5 (659.25 Hz)
      const osc1 = this.audioCtx.createOscillator();
      const gain1 = this.audioCtx.createGain();
      osc1.type = 'sine';
      osc1.frequency.setValueAtTime(659.25, now);
      gain1.gain.setValueAtTime(0.3, now);
      gain1.gain.exponentialRampToValueAtTime(0.001, now + 0.8);
      osc1.connect(gain1);
      gain1.connect(this.audioCtx.destination);
      osc1.start(now);
      osc1.stop(now + 0.8);

      // Note 2: B5 (987.77 Hz)
      const osc2 = this.audioCtx.createOscillator();
      const gain2 = this.audioCtx.createGain();
      osc2.type = 'sine';
      osc2.frequency.setValueAtTime(987.77, now + 0.25);
      gain2.gain.setValueAtTime(0.35, now + 0.25);
      gain2.gain.exponentialRampToValueAtTime(0.001, now + 1.2);
      osc2.connect(gain2);
      gain2.connect(this.audioCtx.destination);
      osc2.start(now + 0.25);
      osc2.stop(now + 1.2);
    } catch {
      // Audio playback permitted upon user interaction
    }
  }

  protected toggleFullscreen(): void {
    if (!document.fullscreenElement) {
      document.documentElement.requestFullscreen().then(() => this.isFullscreen.set(true)).catch(() => {});
    } else {
      document.exitFullscreen().then(() => this.isFullscreen.set(false)).catch(() => {});
    }
  }

  protected maskPatientName(name: string): string {
    if (!name || name.length <= 4) return name || '***';
    const parts = name.trim().split(' ');
    if (parts.length === 1) return parts[0].slice(0, 3) + '...';
    return parts[0] + ' ' + parts[1].slice(0, 1) + '...';
  }

  protected getActiveClinicName(): string {
    const c = this.clinics().find((x) => x.id === this.selectedClinicId);
    if (!c) return this.translation.isArabic() ? 'المركز الطبي المركزي' : 'Central Healthcare Clinic';
    return this.translation.isArabic() && c.nameAr ? c.nameAr : c.name;
  }
}
