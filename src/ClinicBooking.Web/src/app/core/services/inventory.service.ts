import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CreateInventoryItemRequest, MedicalInventoryItemDto } from '../models/inventory.models';

@Injectable({
  providedIn: 'root'
})
export class InventoryService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/inventory`;

  getInventory(clinicId?: string): Observable<MedicalInventoryItemDto[]> {
    let params = new HttpParams();
    if (clinicId) {
      params = params.set('clinicId', clinicId);
    }
    return this.http.get<MedicalInventoryItemDto[]>(this.apiUrl, { params });
  }

  createItem(data: CreateInventoryItemRequest): Observable<MedicalInventoryItemDto> {
    return this.http.post<MedicalInventoryItemDto>(this.apiUrl, data);
  }

  adjustStock(id: string, deltaQuantity: number): Observable<MedicalInventoryItemDto> {
    return this.http.put<MedicalInventoryItemDto>(`${this.apiUrl}/${id}/adjust`, { deltaQuantity });
  }

  deleteItem(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
