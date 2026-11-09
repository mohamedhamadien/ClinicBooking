import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AppointmentDto,
  BookAppointmentRequest,
  CancelAppointmentRequest,
  RescheduleAppointmentRequest
} from '../models/appointment.models';

@Injectable({
  providedIn: 'root'
})
export class AppointmentService {
  private readonly http = inject(HttpClient);

  public bookAppointment(command: BookAppointmentRequest): Observable<AppointmentDto> {
    return this.http.post<AppointmentDto>(`${environment.apiUrl}/appointments`, command);
  }

  public getMyAppointments(): Observable<AppointmentDto[]> {
    return this.http.get<AppointmentDto[]>(`${environment.apiUrl}/appointments/my-appointments`);
  }

  public getAppointmentById(id: string): Observable<AppointmentDto> {
    return this.http.get<AppointmentDto>(`${environment.apiUrl}/appointments/${id}`);
  }

  public cancelAppointment(id: string, reason: string): Observable<void> {
    const payload: CancelAppointmentRequest = { reason };
    return this.http.post<void>(`${environment.apiUrl}/appointments/${id}/cancel`, payload);
  }

  public rescheduleAppointment(
    id: string,
    newStartTimeUtc: string,
    newEndTimeUtc: string
  ): Observable<void> {
    const payload: RescheduleAppointmentRequest = { newStartTimeUtc, newEndTimeUtc };
    return this.http.post<void>(`${environment.apiUrl}/appointments/${id}/reschedule`, payload);
  }
}
