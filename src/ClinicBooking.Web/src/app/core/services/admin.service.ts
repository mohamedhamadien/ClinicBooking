import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { DashboardStatsDto } from '../models/dashboard.models';
import { AppointmentDto, CancelAppointmentRequest } from '../models/appointment.models';
import { ClinicDto, DoctorDto, SpecialtyDto } from '../models/doctor.models';

export interface RegisterDoctorRequest {
  tenantId: string;
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  specialtyId: string;
  licenseNumber: string;
  bioEn: string;
  bioAr?: string;
  clinicId?: string;
  consultationFee?: number;
  defaultSlotDurationMinutes?: number;
  requiresApproval?: boolean;
  initialShiftDay?: number;
  initialShiftStartTime?: string; // HH:mm:ss
  initialShiftEndTime?: string;   // HH:mm:ss
}

export interface UpdateClinicRequest {
  name: string;
  address: string;
  city: string;
  phoneNumber: string;
  isActive?: boolean;
  nameAr?: string;
  addressAr?: string;
  cityAr?: string;
  subscriptionStartDateUtc?: string;
  subscriptionEndDateUtc?: string;
  subscriptionPlan?: string;
}

export interface AuditLogDto {
  id: string;
  tenantId?: string;
  entityName: string;
  entityId: string;
  action: string;
  changedBy: string;
  changedAtUtc: string;
  changesJson?: string;
}

@Injectable({
  providedIn: 'root'
})
export class AdminService {
  private readonly http = inject(HttpClient);

  public getDashboardStats(clinicId?: string): Observable<DashboardStatsDto> {
    let params = new HttpParams();
    if (clinicId) params = params.set('clinicId', clinicId);
    return this.http.get<DashboardStatsDto>(`${environment.apiUrl}/admin/dashboard-stats`, { params });
  }

  public getAllAppointments(
    fromUtc?: string,
    toUtc?: string,
    doctorId?: string,
    clinicId?: string
  ): Observable<AppointmentDto[]> {
    let params = new HttpParams();
    if (fromUtc) params = params.set('fromUtc', fromUtc);
    if (toUtc) params = params.set('toUtc', toUtc);
    if (doctorId) params = params.set('doctorId', doctorId);
    if (clinicId) params = params.set('clinicId', clinicId);

    return this.http.get<AppointmentDto[]>(`${environment.apiUrl}/admin/appointments`, { params });
  }

  public registerDoctor(command: RegisterDoctorRequest): Observable<DoctorDto> {
    return this.http.post<DoctorDto>(`${environment.apiUrl}/doctors/register`, command);
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
    subscriptionStartDateUtc?: string;
    subscriptionEndDateUtc?: string;
    subscriptionPlan?: string;
  }): Observable<ClinicDto> {
    return this.http.post<ClinicDto>(`${environment.apiUrl}/clinics`, command);
  }

  public updateClinic(id: string, command: UpdateClinicRequest): Observable<ClinicDto> {
    return this.http.put<ClinicDto>(`${environment.apiUrl}/clinics/${id}`, command);
  }

  public assignDoctorToClinic(doctorId: string, clinicId: string, consultationFee: number): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}/doctors/${doctorId}/clinics`, {
      clinicId,
      consultationFee,
      isPrimary: true
    });
  }

  public removeDoctorFromClinic(doctorId: string, clinicId: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/doctors/${doctorId}/clinics/${clinicId}`);
  }

  public cancelAppointment(id: string, reason: string): Observable<AppointmentDto> {
    const payload: CancelAppointmentRequest = { reason };
    return this.http.put<AppointmentDto>(`${environment.apiUrl}/appointments/${id}/cancel`, payload);
  }

  public getAuditLogs(entityName?: string, limit: number = 100): Observable<AuditLogDto[]> {
    let params = new HttpParams().set('limit', limit.toString());
    if (entityName) params = params.set('entityName', entityName);
    return this.http.get<AuditLogDto[]>(`${environment.apiUrl}/admin/audit-logs`, { params });
  }
}
