import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DoctorService } from '../../../core/services/doctor.service';
import { TranslationService } from '../../../core/services/translation.service';
import { TranslatePipe } from '../../../core/pipes/translate.pipe';
import { ClinicDto, DoctorDto, SpecialtyDto } from '../../../core/models/doctor.models';

@Component({
  selector: 'app-doctor-list',
  standalone: true,
  imports: [RouterLink, FormsModule, TranslatePipe],
  templateUrl: './doctor-list.html',
  styleUrls: ['./doctor-list.scss']
})
export class DoctorListComponent implements OnInit {
  private readonly doctorService = inject(DoctorService);
  private readonly route = inject(ActivatedRoute);
  protected readonly translation = inject(TranslationService);

  protected readonly doctors = signal<DoctorDto[]>([]);
  protected readonly specialties = signal<SpecialtyDto[]>([]);
  protected readonly clinics = signal<ClinicDto[]>([]);
  protected readonly loading = signal(true);

  // Filters
  protected selectedSpecialty = '';
  protected selectedClinic = '';
  protected searchTerm = '';

  ngOnInit(): void {
    // Check query params (e.g. ?specialtyId=...)
    this.route.queryParams.subscribe((params) => {
      if (params['specialtyId']) {
        this.selectedSpecialty = params['specialtyId'];
      }
      this.loadData();
    });
  }

  protected loadData(): void {
    this.loading.set(true);

    this.doctorService.getSpecialties().subscribe((data) => this.specialties.set(data));
    this.doctorService.getClinics().subscribe((data) => this.clinics.set(data));

    this.applyFilters();
  }

  protected applyFilters(): void {
    this.loading.set(true);
    this.doctorService
      .getDoctors(
        this.selectedSpecialty || undefined,
        this.selectedClinic || undefined,
        this.searchTerm || undefined
      )
      .subscribe({
        next: (data) => {
          this.doctors.set(data);
          this.loading.set(false);
        },
        error: () => this.loading.set(false)
      });
  }
}
