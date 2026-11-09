import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { TranslatePipe } from '../../../core/pipes/translate.pipe';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, TranslatePipe],
  templateUrl: './login.html',
  styleUrls: ['./login.scss']
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly errorMessage = signal<string | null>(null);
  protected readonly isSubmitting = signal(false);

  protected readonly form = this.fb.group({
    email: ['admin@clinicbooking.com', [Validators.required, Validators.email]],
    password: ['Admin@Secure123!', [Validators.required]]
  });

  protected onSubmit(): void {
    if (this.form.invalid) return;

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    const { email, password } = this.form.value;

    this.authService.login({ email: email!, password: password! }).subscribe({
      next: (res) => {
        this.isSubmitting.set(false);
        if (res.user.role === 'Admin') {
          this.router.navigate(['/admin/dashboard']);
        } else if (res.user.role === 'Patient') {
          this.router.navigate(['/my-appointments']);
        } else {
          this.router.navigate(['/doctors']);
        }
      },
      error: (err) => {
        this.isSubmitting.set(false);
        this.errorMessage.set(
          err.error?.detail || err.error?.message || 'Invalid email or password.'
        );
      }
    });
  }
}
