export interface SpecialtyDto {
  id: string;
  name: string;
  description: string;
  nameAr?: string;
  descriptionAr?: string;
  tenantId?: string;
}

export interface ClinicDto {
  id: string;
  tenantId: string;
  name: string;
  address: string;
  city: string;
  phoneNumber: string;
  nameAr?: string;
  addressAr?: string;
  cityAr?: string;
  isActive: boolean;
  subscriptionStartDateUtc?: string;
  subscriptionEndDateUtc?: string;
  subscriptionPlan?: string;
  isSubscriptionActive?: boolean;
  subscriptionDaysRemaining?: number;
  isPrivateClinic?: boolean;
  isSingleDoctorClinic?: boolean;
  enableProceduresDuringShifts?: boolean;
  enableGeneralClinicFeatures?: boolean;
  consultationFee?: number;
  followUpFee?: number;
  procedureFee?: number;
}

export interface DoctorShiftDto {
  startTime: string;
  endTime: string;
}

export interface DoctorAvailabilityDto {
  id: string;
  clinicId: string;
  clinicName?: string;
  dayOfWeek: number;
  dayName: string;
  shift: DoctorShiftDto;
}

export interface DoctorClinicDto {
  clinicId: string;
  clinicName: string;
  clinicCity: string;
  consultationFee?: number;
}

export interface DoctorAvailabilityExceptionDto {
  id: string;
  startUtc: string;
  endUtc: string;
  reason: string;
  isBlocked: boolean;
}

export interface SetDoctorAvailabilityRequest {
  clinicId: string;
  dayOfWeek: number; // 0 = Sunday, 1 = Monday, ..., 5 = Friday, 6 = Saturday
  startTime: string; // HH:mm:ss
  endTime: string;   // HH:mm:ss
}

export interface AddAvailabilityExceptionRequest {
  startUtc: string;
  endUtc: string;
  reason: string;
}

export interface DoctorDto {
  id: string;
  userId: string;
  tenantId: string;
  specialtyId: string;
  specialtyName: string;
  licenseNumber: string;
  fullName: string;
  email: string;
  bio: string;
  bioAr?: string;
  defaultSlotDurationMinutes: number;
  requiresApproval: boolean;
  isActive: boolean;
  doctorClinics: DoctorClinicDto[];
  availabilities: DoctorAvailabilityDto[];
  exceptions?: DoctorAvailabilityExceptionDto[];
}

export interface AvailableSlotDto {
  doctorId?: string;
  clinicId?: string;
  startUtc?: string;
  endUtc?: string;
  startTimeUtc: string;
  endTimeUtc: string;
  startLocal: string;
  endLocal: string;
  durationMinutes: number;
  isAvailable: boolean;
  dayOfWeekName?: string;
}
