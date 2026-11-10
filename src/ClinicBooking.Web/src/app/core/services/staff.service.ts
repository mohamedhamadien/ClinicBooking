import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CreateStaffRequest,
  PermissionCatalogDto,
  StaffMemberDto,
  UpdateStaffPermissionsRequest
} from '../models/staff.models';

@Injectable({
  providedIn: 'root'
})
export class StaffService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/staff`;

  public getStaff(clinicId?: string): Observable<StaffMemberDto[]> {
    let params = new HttpParams();
    if (clinicId) {
      params = params.set('clinicId', clinicId);
    }
    return this.http.get<StaffMemberDto[]>(this.baseUrl, { params });
  }

  public getPermissionsCatalog(): Observable<PermissionCatalogDto> {
    return this.http.get<PermissionCatalogDto>(`${this.baseUrl}/permissions-catalog`);
  }

  public createStaff(request: CreateStaffRequest): Observable<StaffMemberDto> {
    return this.http.post<StaffMemberDto>(this.baseUrl, request);
  }

  public updatePermissions(id: string, request: UpdateStaffPermissionsRequest): Observable<StaffMemberDto> {
    return this.http.put<StaffMemberDto>(`${this.baseUrl}/${id}/permissions`, request);
  }

  public toggleStatus(id: string): Observable<StaffMemberDto> {
    return this.http.put<StaffMemberDto>(`${this.baseUrl}/${id}/status`, {});
  }
}
