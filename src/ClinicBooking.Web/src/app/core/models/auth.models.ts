export type UserRole = 'Admin' | 'ClinicAdmin' | 'Doctor' | 'Patient';

export interface UserDto {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: UserRole;
  tenantId?: string;
  clinicId?: string;
}

export interface AuthResponseDto {
  accessToken: string;
  refreshToken: string;
  user: UserDto;
}

export interface LoginRequest {
  email: string;
  password: string;
  tenantIdentifier?: string;
}

export interface RegisterPatientRequest {
  tenantId: string;
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  dateOfBirth: string; // YYYY-MM-DD
  gender: number;      // 1 = Male, 2 = Female, 3 = Other
  emergencyContact?: string;
}

export interface RefreshTokenRequest {
  accessToken: string;
  refreshToken: string;
}
