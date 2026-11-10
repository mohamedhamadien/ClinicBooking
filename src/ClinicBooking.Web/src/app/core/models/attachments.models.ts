export interface MedicalAttachmentDto {
  id: string;
  patientId: string;
  title: string;
  attachmentType: 'LabResult' | 'XRay' | 'MRI' | 'Ultrasound' | 'PrescriptionScan' | 'Report' | 'Other' | string;
  fileName: string;
  contentType: string;
  fileSizeBytes: number;
  dataUrl: string;
  notes?: string;
  uploadedAtUtc: string;
}

export interface UploadAttachmentRequest {
  patientId: string;
  title: string;
  attachmentType: string;
  fileName: string;
  contentType: string;
  fileSizeBytes: number;
  dataUrl: string;
  notes?: string;
}
