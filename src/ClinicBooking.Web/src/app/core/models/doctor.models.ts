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
}

export interface AvailableSlotDto {
  startTimeUtc: string;
  endTimeUtc: string;
  startLocal: string;
  endLocal: string;
  durationMinutes: number;
  isAvailable: boolean;
}
