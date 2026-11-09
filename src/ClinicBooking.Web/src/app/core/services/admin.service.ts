import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { DashboardStatsDto } from '../models/dashboard.models';
import { AppointmentDto, CancelAppointmentRequest } from '../models/appointment.models';
import { ClinicDto, SpecialtyDto } from '../models/doctor.models';

@Injectable({
  providedIn: 'root'
})
export class AdminService {
  private readonly http = inject(HttpClient);

  public getDashboardStats(): Observable<DashboardStatsDto> {
    return this.http.get<DashboardStatsDto>(`${environment.apiUrl}/admin/dashboard`);
  }

  public getAllAppointments(
    fromUtc?: string,
    toUtc?: string,
    doctorId?: string
  ): Observable<AppointmentDto[]> {
    let params = new HttpParams();
    if (fromUtc) params = params.set('fromUtc', fromUtc);
    if (toUtc) params = params.set('toUtc', toUtc);
    if (doctorId) params = params.set('doctorId', doctorId);

    return this.http.get<AppointmentDto[]>(`${environment.apiUrl}/admin/appointments`, { params });
  }

  public createSpecialty(command: {
    name: string;
    description: string;
    nameAr?: string;
    descriptionAr?: string;
  }): Observable<SpecialtyDto> {
    return this.http.post<SpecialtyDto>(`${environment.apiUrl}/specialties`, command);
  }

  public createClinic(command: {
    tenantId: string;
    name: string;
    address: string;
    city: string;
    phoneNumber: string;
    nameAr?: string;
    addressAr?: string;
    cityAr?: string;
  }): Observable<ClinicDto> {
    return this.http.post<ClinicDto>(`${environment.apiUrl}/clinics`, command);
  }

  public cancelAppointment(id: string, reason: string): Observable<AppointmentDto> {
    const payload: CancelAppointmentRequest = { reason };
    return this.http.put<AppointmentDto>(`${environment.apiUrl}/appointments/${id}/cancel`, payload);
  }
}
