export interface InsuranceProviderDto {
  id: string;
  name: string;
  nameAr?: string;
  code: string;
  defaultCoveragePercentage: number;
  contactPhone?: string;
  isActive: boolean;
}

export interface CreateInsuranceProviderRequest {
  name: string;
  nameAr?: string;
  code: string;
  defaultCoveragePercentage: number;
  contactPhone?: string;
}
