import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '../../core/pipes/translate.pipe';
import { DoctorService } from '../../core/services/doctor.service';
import { SpecialtyDto } from '../../core/models/doctor.models';
import { TranslationService } from '../../core/services/translation.service';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterLink, TranslatePipe],
  templateUrl: './home.html',
  styleUrls: ['./home.scss']
})
export class HomeComponent implements OnInit {
  private readonly doctorService = inject(DoctorService);
  protected readonly translation = inject(TranslationService);

  protected readonly specialties = signal<SpecialtyDto[]>([]);
  protected readonly loading = signal(true);

  ngOnInit(): void {
    this.doctorService.getSpecialties().subscribe({
      next: (data) => {
        this.specialties.set(data);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }
}
