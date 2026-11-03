import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../config/environment/environment';

export interface PractitionerClient {
  id: string;
  fullName: string;
  surname?: string | null;
  fatherName?: string | null;
  motherName?: string | null;
  gotra?: string | null;
  gender: string;
  dateOfBirth: string;
  timeOfBirth: string;
  birthPlace: string;
  latitude?: number | null;
  longitude?: number | null;
  phoneNumber?: string | null;
  email?: string | null;
  topic?: string | null;
  status: CrmStatus;
  notes?: string | null;
  linkedChartCount: number;
  nextAppointment?: PractitionerAppointment | null;
  isArchived: boolean;
  charts: PractitionerClientChart[];
  appointments: PractitionerAppointment[];
}

export interface PractitionerClientChart {
  id: string;
  fullName: string;
  dateOfBirth: string;
  timeOfBirth: string;
  birthPlace: string;
}

export interface PractitionerClientSaveRequest {
  fullName: string;
  surname?: string | null;
  fatherName?: string | null;
  motherName?: string | null;
  gotra?: string | null;
  gender: string;
  dateOfBirth: string;
  timeOfBirth: string;
  birthPlace: string;
  latitude?: number | null;
  longitude?: number | null;
  phoneNumber?: string | null;
  email?: string | null;
  topic?: string | null;
  status: CrmStatus;
  notes?: string | null;
}

export type CrmStatus = 'New' | 'Consultation Pending' | 'Completed' | 'Cancelled';

export interface CrmDashboardSummary {
  activeClients: number;
  allSavedCharts: number;
  linkedClientCharts: number;
  upcomingConsultations: number;
  completedConsultationsLast30Days: number;
  todaysConsultations: number;
}

export interface PractitionerAppointment {
  id: string;
  clientId: string;
  clientName?: string | null;
  kundaliId?: string | null;
  kundaliName?: string | null;
  startsAtLocal: string;
  endsAtLocal: string;
  startsAtUtc: string;
  endsAtUtc: string;
  timeZoneId: string;
  status: 'New' | 'Consultation Pending' | 'Completed' | 'Cancelled' | 'Scheduled';
  topic?: string | null;
  completedAtUtc?: string | null;
  reminderLeadMinutes?: number | null;
  notes?: string | null;
  overlapsExisting?: boolean;
}

export interface AppointmentRequest {
  clientId: string;
  topic?: string | null;
  status: Exclude<CrmStatus, 'Cancelled'>;
  kundaliId?: string | null;
  startsAtLocal: string;
  endsAtLocal: string;
  timeZoneId: string;
  reminderLeadMinutes?: number | null;
  notes?: string | null;
}

export interface CrmNotification {
  id: string;
  type: string;
  title: string;
  message: string;
  link: string;
  createdAt: string;
  isRead: boolean;
}

@Injectable({ providedIn: 'root' })
export class PractitionerCrmService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}`;

  getClients(search = ''): Observable<PractitionerClient[]> {
    const url = `${this.baseUrl}/clients`;
    return this.http.get<PractitionerClient[]>(url, { params: search ? { search } : {} });
  }

  getDashboardSummary(timeZoneId: string): Observable<CrmDashboardSummary> {
    return this.http.get<CrmDashboardSummary>(`${this.baseUrl}/clients/dashboard-summary`, { params: { timeZoneId } });
  }

  getClient(id: string): Observable<PractitionerClient> {
    return this.http.get<PractitionerClient>(`${this.baseUrl}/clients/${id}`);
  }

  createClient(value: PractitionerClientSaveRequest): Observable<PractitionerClient> {
    return this.http.post<PractitionerClient>(`${this.baseUrl}/clients`, value);
  }

  updateClient(id: string, value: PractitionerClientSaveRequest): Observable<PractitionerClient> {
    return this.http.put<PractitionerClient>(`${this.baseUrl}/clients/${id}`, value);
  }

  archiveClient(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/clients/${id}/archive`, {});
  }

  linkChart(clientId: string, chartId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/clients/${clientId}/charts/${chartId}`, {});
  }

  unlinkChart(clientId: string, chartId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/clients/${clientId}/charts/${chartId}`);
  }

  getAppointments(): Observable<PractitionerAppointment[]> {
    return this.http.get<PractitionerAppointment[]>(`${this.baseUrl}/appointments`);
  }

  createAppointment(value: AppointmentRequest): Observable<PractitionerAppointment> {
    return this.http.post<PractitionerAppointment>(`${this.baseUrl}/appointments`, value);
  }

  updateAppointment(id: string, value: AppointmentRequest): Observable<PractitionerAppointment> {
    return this.http.put<PractitionerAppointment>(`${this.baseUrl}/appointments/${id}`, value);
  }

  cancelAppointment(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/appointments/${id}/cancel`, {});
  }

  completeAppointment(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/appointments/${id}/complete`, {});
  }

  getNotifications(): Observable<CrmNotification[]> {
    return this.http.get<CrmNotification[]>(`${this.baseUrl}/notifications`);
  }

  markNotificationRead(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/notifications/${id}/read`, {});
  }

  markAllNotificationsRead(): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/notifications/read-all`, {});
  }
}
