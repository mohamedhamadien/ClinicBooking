export interface StaffMemberDto {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  role: string;
  isActive: boolean;
  clinicId?: string;
  clinicName?: string;
  permissions: string[];
  createdAtUtc: string;
}

export interface PermissionItemDto {
  key: string;
  nameEn: string;
  nameAr: string;
  category: string;
  description: string;
}

export interface PermissionCatalogDto {
  permissions: PermissionItemDto[];
  rolePresets: Record<string, string[]>;
}

export interface CreateStaffRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  role: string;
  clinicId?: string;
  permissions?: string[];
}

export interface UpdateStaffPermissionsRequest {
  role?: string;
  clinicId?: string;
  permissions?: string[];
}
