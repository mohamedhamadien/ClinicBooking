import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AddAvailabilityExceptionRequest,
  AvailableSlotDto,
  ClinicDto,
  DoctorDto,
  SetDoctorAvailabilityRequest,
  SpecialtyDto
} from '../models/doctor.models';

@Injectable({
  providedIn: 'root'
})
export class DoctorService {
  private readonly http = inject(HttpClient);

  public getDoctors(
    specialtyId?: string,
    clinicId?: string,
    searchTerm?: string
  ): Observable<DoctorDto[]> {
    let params = new HttpParams();
    if (specialtyId) params = params.set('specialtyId', specialtyId);
    if (clinicId) params = params.set('clinicId', clinicId);
    if (searchTerm) params = params.set('searchTerm', searchTerm);

    return this.http.get<DoctorDto[]>(`${environment.apiUrl}/doctors`, { params });
  }

  public getDoctorById(id: string): Observable<DoctorDto> {
    return this.http.get<DoctorDto>(`${environment.apiUrl}/doctors/${id}`);
  }

  public getSpecialties(): Observable<SpecialtyDto[]> {
    return this.http.get<SpecialtyDto[]>(`${environment.apiUrl}/specialties`);
  }

  public getClinics(): Observable<ClinicDto[]> {
    return this.http.get<ClinicDto[]>(`${environment.apiUrl}/clinics`);
  }

  public getAvailableSlots(
    doctorId: string,
    clinicId: string,
    date: string // YYYY-MM-DD
  ): Observable<AvailableSlotDto[]> {
    const params = new HttpParams()
      .set('doctorId', doctorId)
      .set('clinicId', clinicId)
      .set('date', date);

    return this.http.get<AvailableSlotDto[]>(
      `${environment.apiUrl}/appointments/available-slots`,
      { params }
    );
  }

  public setDoctorAvailability(
    doctorId: string,
    request: SetDoctorAvailabilityRequest
  ): Observable<void> {
    return this.http.post<void>(
      `${environment.apiUrl}/doctors/${doctorId}/availability`,
      request
    );
  }

  public removeDoctorAvailability(
    doctorId: string,
    availabilityId: string
  ): Observable<void> {
    return this.http.delete<void>(
      `${environment.apiUrl}/doctors/${doctorId}/availability/${availabilityId}`
    );
  }

  public addAvailabilityException(
    doctorId: string,
    request: AddAvailabilityExceptionRequest
  ): Observable<void> {
    return this.http.post<void>(
      `${environment.apiUrl}/doctors/${doctorId}/availability-exceptions`,
      request
    );
  }

  public removeAvailabilityException(
    doctorId: string,
    exceptionId: string
  ): Observable<void> {
    return this.http.delete<void>(
      `${environment.apiUrl}/doctors/${doctorId}/availability-exceptions/${exceptionId}`
    );
  }
}
