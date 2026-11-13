export type AppointmentStatus =
  | 'Scheduled'
  | 'Confirmed'
  | 'CheckedIn'
  | 'InConsultation'
  | 'Completed'
  | 'CancelledByPatient'
  | 'CancelledByDoctor'
  | 'CancelledByAdmin'
  | 'Rejected'
  | 'NoShow';

export type AppointmentSource =
  | 'Website'
  | 'Facebook'
  | 'Messenger'
  | 'WhatsApp'
  | 'Phone'
  | 'WalkIn'
  | 'Receptionist'
  | 'Admin';

export interface AppointmentDto {
  id: string;
  tenantId: string;
  doctorId: string;
  doctorName?: string;
  patientId: string;
  patientName?: string;
  clinicId: string;
  clinicName?: string;
  startTimeUtc: string;
  endTimeUtc: string;
  status: AppointmentStatus;
  statusLocalized?: string;
  notes?: string;
  cancellationReason?: string;
  createdAtUtc: string;
  source?: AppointmentSource | string;
  createdByStaffId?: string;
  utmSource?: string;
  utmMedium?: string;
  utmCampaign?: string;
  externalReference?: string;
  queueNumber?: number;
  visitType?: 'Consultation' | 'FollowUp' | 'Procedure' | string;
  procedureName?: string;
  agreedFee?: number;
}

export interface BookAppointmentRequest {
  tenantId: string;
  doctorId: string;
  patientId: string;
  clinicId: string;
  startTimeUtc: string;
  endTimeUtc: string;
  notes?: string;
  visitType?: 'Consultation' | 'FollowUp' | 'Procedure' | string;
  procedureName?: string;
  agreedFee?: number;
}

export interface CancelAppointmentRequest {
  reason: string;
}

export interface RescheduleAppointmentRequest {
  newStartTimeUtc: string;
  newEndTimeUtc: string;
}
