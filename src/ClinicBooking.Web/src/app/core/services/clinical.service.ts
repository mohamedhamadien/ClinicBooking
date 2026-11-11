import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ConsultationDto,
  InvoiceDto,
  PatientMedicalRecordDto,
  PatientSummaryDto,
  CreatePatientRequest,
  PaymentMethod,
  PrescriptionDto,
  QueueItemDto
} from '../models/clinical.models';

@Injectable({
  providedIn: 'root'
})
export class ClinicalService {
  private readonly http = inject(HttpClient);

  // --- Queue & Reception ---
  public checkIn(appointmentId: string): Observable<QueueItemDto> {
    return this.http.post<QueueItemDto>(`${environment.apiUrl}/queue/check-in`, { appointmentId });
  }

  public callPatient(appointmentId: string): Observable<QueueItemDto> {
    return this.http.post<QueueItemDto>(`${environment.apiUrl}/queue/${appointmentId}/call`, {});
  }

  public getClinicQueue(clinicId: string, date?: string): Observable<QueueItemDto[]> {
    let params = new HttpParams();
    if (date) params = params.set('date', date);
    return this.http.get<QueueItemDto[]>(`${environment.apiUrl}/queue/clinic/${clinicId}`, { params });
  }

  // --- Consultations & EMR ---
  public createConsultation(command: {
    tenantId: string;
    appointmentId: string;
    patientId: string;
    doctorId: string;
    clinicId: string;
    chiefComplaint: string;
    examinationNotes: string;
    diagnosis: string;
    treatmentPlan: string;
    vitalSigns?: {
      systolicBp?: number;
      diastolicBp?: number;
      heartRate?: number;
      temperatureCelsius?: number;
      weightKg?: number;
      heightCm?: number;
      bloodGlucoseMgDl?: number;
    };
    followUpDateUtc?: string;
    labRequests?: string;
  }): Observable<ConsultationDto> {
    return this.http.post<ConsultationDto>(`${environment.apiUrl}/consultations`, command);
  }

  // --- Patients Management ---
  public getPatients(search?: string): Observable<PatientSummaryDto[]> {
    let params = new HttpParams();
    if (search) params = params.set('search', search);
    return this.http.get<PatientSummaryDto[]>(`${environment.apiUrl}/patients`, { params });
  }

  public createPatient(data: CreatePatientRequest): Observable<PatientSummaryDto> {
    return this.http.post<PatientSummaryDto>(`${environment.apiUrl}/patients`, data);
  }

  public getPatientMedicalRecord(patientId: string): Observable<PatientMedicalRecordDto> {
    return this.http.get<PatientMedicalRecordDto>(`${environment.apiUrl}/consultations/patients/${patientId}`);
  }

  public updateMedicalHistory(patientId: string, history: {
    bloodGroup?: string;
    allergies?: string;
    chronicConditions?: string;
    medicalNotes?: string;
  }): Observable<void> {
    return this.http.put<void>(`${environment.apiUrl}/consultations/patients/${patientId}/medical-history`, history);
  }

  // --- Prescriptions (Rx) ---
  public createPrescription(command: {
    tenantId: string;
    consultationId?: string;
    patientId: string;
    doctorId: string;
    clinicId: string;
    diagnosis: string;
    generalInstructions?: string;
    items?: {
      medicationName: string;
      dosage: string;
      frequency: string;
      duration: string;
      instructions?: string;
    }[];
  }): Observable<PrescriptionDto> {
    return this.http.post<PrescriptionDto>(`${environment.apiUrl}/prescriptions`, command);
  }

  public getPatientPrescriptions(patientId: string): Observable<PrescriptionDto[]> {
    return this.http.get<PrescriptionDto[]>(`${environment.apiUrl}/prescriptions/patients/${patientId}`);
  }

  // --- Invoicing & Billing ---
  public getInvoices(clinicId?: string): Observable<InvoiceDto[]> {
    let params = new HttpParams();
    if (clinicId) params = params.set('clinicId', clinicId);
    return this.http.get<InvoiceDto[]>(`${environment.apiUrl}/invoices`, { params });
  }

  public createInvoice(command: {
    tenantId: string;
    appointmentId?: string;
    patientId: string;
    clinicId: string;
    discountAmount?: number;
    taxAmount?: number;
    paymentMethod?: number;
    insuranceProviderId?: string;
    insuranceCoverageAmount?: number;
    patientCopayAmount?: number;
    items?: {
      description: string;
      quantity: number;
      unitPrice: number;
    }[];
  }): Observable<InvoiceDto> {
    return this.http.post<InvoiceDto>(`${environment.apiUrl}/invoices`, command);
  }

  public payInvoice(invoiceId: string, paymentMethod: number, amount?: number): Observable<InvoiceDto> {
    const payload: { paymentMethod: number; amount?: number } = { paymentMethod };
    if (amount !== undefined && amount > 0) {
      payload.amount = amount;
    }
    return this.http.post<InvoiceDto>(`${environment.apiUrl}/invoices/${invoiceId}/pay`, payload);
  }
}
