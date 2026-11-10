export interface PatientVitalSignsDto {
  id: string;
  patientId: string;
  consultationId?: string;
  systolicBp?: number;
  diastolicBp?: number;
  heartRate?: number;
  temperatureCelsius?: number;
  weightKg?: number;
  heightCm?: number;
  bloodGlucoseMgDl?: number;
  bmi?: number;
  recordedAtUtc: string;
}

export interface ConsultationDto {
  id: string;
  appointmentId: string;
  patientId: string;
  doctorId: string;
  clinicId: string;
  chiefComplaint: string;
  examinationNotes: string;
  diagnosis: string;
  treatmentPlan: string;
  consultationDateUtc: string;
  vitalSigns?: PatientVitalSignsDto;
  doctorName?: string;
  patientName?: string;
  clinicName?: string;
  insuranceProviderId?: string;
  insuranceProviderName?: string;
  insuranceCoverageAmount?: number;
  patientCopayAmount?: number;
  followUpDateUtc?: string;
  labRequests?: string;
}

export interface PrescriptionItemDto {
  id: string;
  medicationName: string;
  dosage: string;
  frequency: string;
  duration: string;
  instructions?: string;
}

export interface PrescriptionDto {
  id: string;
  tenantId: string;
  consultationId?: string;
  patientId: string;
  doctorId: string;
  clinicId: string;
  prescriptionNumber: string;
  diagnosis: string;
  generalInstructions?: string;
  issuedAtUtc: string;
  items: PrescriptionItemDto[];
  doctorName?: string;
  patientName?: string;
  clinicName?: string;
  insuranceProviderId?: string;
  insuranceProviderName?: string;
  insuranceCoverageAmount?: number;
  patientCopayAmount?: number;
  followUpDateUtc?: string;
  labRequests?: string;
}

export interface PatientMedicalRecordDto {
  patientId: string;
  patientName: string;
  dateOfBirth: string;
  gender: string;
  bloodGroup?: string;
  allergies?: string;
  chronicConditions?: string;
  medicalNotes?: string;
  consultations: ConsultationDto[];
  vitalSignsHistory: PatientVitalSignsDto[];
  prescriptions: PrescriptionDto[];
}

export type InvoiceStatus = 'Pending' | 'Paid' | 'Cancelled' | 'Refunded' | 'PartiallyPaid';
export type PaymentMethod = 'Cash' | 'CreditCard' | 'Insurance' | 'BankTransfer';

export interface InvoiceItemDto {
  id: string;
  description: string;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
}

export interface InvoiceDto {
  id: string;
  tenantId: string;
  appointmentId?: string;
  patientId: string;
  clinicId: string;
  invoiceNumber: string;
  subTotal: number;
  discountAmount: number;
  taxAmount: number;
  totalAmount: number;
  paidAmount: number;
  remainingAmount?: number;
  status: InvoiceStatus;
  paymentMethod: PaymentMethod;
  paidAtUtc?: string;
  issuedAtUtc: string;
  items: InvoiceItemDto[];
  patientName?: string;
  clinicName?: string;
  insuranceProviderId?: string;
  insuranceProviderName?: string;
  insuranceCoverageAmount?: number;
  patientCopayAmount?: number;
  followUpDateUtc?: string;
  labRequests?: string;
}

export interface QueueItemDto {
  appointmentId: string;
  queueNumber?: number;
  patientId: string;
  patientName: string;
  doctorId: string;
  doctorName: string;
  clinicId: string;
  clinicName: string;
  startTimeUtc: string;
  status: string;
}
