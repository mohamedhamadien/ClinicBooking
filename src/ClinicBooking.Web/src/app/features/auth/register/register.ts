import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { TranslationService } from '../../../core/services/translation.service';
import { TranslatePipe } from '../../../core/pipes/translate.pipe';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, TranslatePipe],
  templateUrl: './register.html',
  styleUrls: ['../login/login.scss']
})
export class RegisterComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  protected readonly translation = inject(TranslationService);

  protected readonly errorMessage = signal<string | null>(null);
  protected readonly isSubmitting = signal(false);

  // Default seeded apex-health tenant id or can be resolved from API
  private readonly defaultTenantId = '1f2aa19b-f5dc-4166-abe6-dc93a2f3aaff';

  protected readonly form = this.fb.group({
    firstName: ['', [Validators.required, Validators.maxLength(50)]],
    lastName: ['', [Validators.required, Validators.maxLength(50)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
    dateOfBirth: ['1995-05-15', [Validators.required]],
    gender: [1, [Validators.required]],
    emergencyContact: ['+201011112222']
  });

  protected onSubmit(): void {
    if (this.form.invalid) return;

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    const val = this.form.value;

    this.authService
      .registerPatient({
        tenantId: this.defaultTenantId,
        firstName: val.firstName!,
        lastName: val.lastName!,
        email: val.email!,
        password: val.password!,
        dateOfBirth: val.dateOfBirth!,
        gender: Number(val.gender!),
        emergencyContact: val.emergencyContact || undefined
      })
      .subscribe({
        next: () => {
          this.isSubmitting.set(false);
          this.router.navigate(['/doctors']);
        },
        error: (err) => {
          this.isSubmitting.set(false);
          this.errorMessage.set(
            err.error?.detail || err.error?.message || 'Registration failed. Please try again.'
          );
        }
      });
  }
}
