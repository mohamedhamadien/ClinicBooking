import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { MedicalAttachmentDto, UploadAttachmentRequest } from '../models/attachments.models';

@Injectable({
  providedIn: 'root'
})
export class AttachmentsService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/attachments`;

  getPatientAttachments(patientId: string): Observable<MedicalAttachmentDto[]> {
    return this.http.get<MedicalAttachmentDto[]>(`${this.apiUrl}/patient/${patientId}`);
  }

  uploadAttachment(data: UploadAttachmentRequest): Observable<MedicalAttachmentDto> {
    return this.http.post<MedicalAttachmentDto>(this.apiUrl, data);
  }

  deleteAttachment(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
