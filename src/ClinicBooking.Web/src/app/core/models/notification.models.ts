export interface NotificationLogDto {
  id: string;
  appointmentId?: string;
  recipientName: string;
  recipientPhone?: string;
  recipientEmail?: string;
  channel: 'WhatsApp' | 'Sms' | 'Email';
  type: string;
  status: 'Sent' | 'Delivered' | 'Failed' | 'Pending';
  subject?: string;
  message: string;
  sentAtUtc: string;
  errorMessage?: string;
}

export interface WhatsAppReminderLinkDto {
  appointmentId: string;
  recipientName: string;
  recipientPhone: string;
  formattedUrl: string;
  messageText: string;
}

export interface SendReminderRequest {
  appointmentId: string;
  channel: 'WhatsApp' | 'Sms' | 'Email';
  customMessage?: string;
}
