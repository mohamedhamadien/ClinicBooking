import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { NotificationLogDto, WhatsAppReminderLinkDto, SendReminderRequest } from '../models/notification.models';

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private readonly http = inject(HttpClient);

  public sendReminder(appointmentId: string, channel: 'WhatsApp' | 'Sms' | 'Email', customMessage?: string): Observable<NotificationLogDto> {
    const payload: SendReminderRequest = { appointmentId, channel, customMessage };
    return this.http.post<NotificationLogDto>(`${environment.apiUrl}/notifications/remind`, payload);
  }

  public getWhatsAppLink(appointmentId: string): Observable<WhatsAppReminderLinkDto> {
    return this.http.get<WhatsAppReminderLinkDto>(`${environment.apiUrl}/notifications/whatsapp-link/${appointmentId}`);
  }

  public getLogs(
    pageNumber = 1,
    pageSize = 20,
    channel?: string,
    status?: string,
    search?: string
  ): Observable<PagedResult<NotificationLogDto>> {
    let params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());

    if (channel) params = params.set('channel', channel);
    if (status) params = params.set('status', status);
    if (search) params = params.set('search', search);

    return this.http.get<PagedResult<NotificationLogDto>>(`${environment.apiUrl}/notifications`, { params });
  }
}
