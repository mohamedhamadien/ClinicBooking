import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CreateInsuranceProviderRequest, InsuranceProviderDto } from '../models/insurance.models';

@Injectable({
  providedIn: 'root'
})
export class InsuranceService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/insurance`;

  getProviders(): Observable<InsuranceProviderDto[]> {
    return this.http.get<InsuranceProviderDto[]>(`${this.apiUrl}/providers`);
  }

  createProvider(data: CreateInsuranceProviderRequest): Observable<InsuranceProviderDto> {
    return this.http.post<InsuranceProviderDto>(`${this.apiUrl}/providers`, data);
  }
}
