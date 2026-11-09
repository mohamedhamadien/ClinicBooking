import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap, catchError, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AuthResponseDto,
  LoginRequest,
  RegisterPatientRequest,
  UserDto
} from '../models/auth.models';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  private readonly ACCESS_TOKEN_KEY = 'cb_access_token';
  private readonly REFRESH_TOKEN_KEY = 'cb_refresh_token';
  private readonly USER_KEY = 'cb_user';

  // State Signals
  public readonly currentUser = signal<UserDto | null>(this.getStoredUser());
  public readonly accessToken = signal<string | null>(localStorage.getItem(this.ACCESS_TOKEN_KEY));

  // Computed Roles
  public readonly isAuthenticated = computed(() => !!this.accessToken() && !!this.currentUser());
  public readonly isAdmin = computed(() => this.currentUser()?.role === 'Admin');
  public readonly isDoctor = computed(() => this.currentUser()?.role === 'Doctor');
  public readonly isPatient = computed(() => this.currentUser()?.role === 'Patient');

  public login(request: LoginRequest): Observable<AuthResponseDto> {
    return this.http.post<AuthResponseDto>(`${environment.apiUrl}/auth/login`, request).pipe(
      tap((response) => this.setSession(response))
    );
  }

  public registerPatient(request: RegisterPatientRequest): Observable<AuthResponseDto> {
    return this.http.post<AuthResponseDto>(`${environment.apiUrl}/auth/register`, request).pipe(
      tap((response) => this.setSession(response))
    );
  }

  public refreshToken(): Observable<AuthResponseDto> {
    const access = this.getAccessToken();
    const refresh = this.getRefreshToken();

    if (!access || !refresh) {
      this.logout();
      return throwError(() => new Error('No refresh token available.'));
    }

    return this.http
      .post<AuthResponseDto>(`${environment.apiUrl}/auth/refresh-token`, {
        accessToken: access,
        refreshToken: refresh
      })
      .pipe(
        tap((response) => this.setSession(response)),
        catchError((err) => {
          this.logout();
          return throwError(() => err);
        })
      );
  }

  public logout(): void {
    localStorage.removeItem(this.ACCESS_TOKEN_KEY);
    localStorage.removeItem(this.REFRESH_TOKEN_KEY);
    localStorage.removeItem(this.USER_KEY);
    this.accessToken.set(null);
    this.currentUser.set(null);
    this.router.navigate(['/auth/login']);
  }

  public getAccessToken(): string | null {
    return localStorage.getItem(this.ACCESS_TOKEN_KEY);
  }

  public getRefreshToken(): string | null {
    return localStorage.getItem(this.REFRESH_TOKEN_KEY);
  }

  private setSession(authResponse: AuthResponseDto): void {
    const token = authResponse.accessToken || (authResponse as any).AccessToken;
    const refreshToken = authResponse.refreshToken || (authResponse as any).RefreshToken;
    const user = authResponse.user || (authResponse as any).User;

    localStorage.setItem(this.ACCESS_TOKEN_KEY, token);
    localStorage.setItem(this.REFRESH_TOKEN_KEY, refreshToken);
    localStorage.setItem(this.USER_KEY, JSON.stringify(user));
    this.accessToken.set(token);
    this.currentUser.set(user);
  }

  private getStoredUser(): UserDto | null {
    const json = localStorage.getItem(this.USER_KEY);
    if (!json) return null;
    try {
      return JSON.parse(json) as UserDto;
    } catch {
      return null;
    }
  }
}
