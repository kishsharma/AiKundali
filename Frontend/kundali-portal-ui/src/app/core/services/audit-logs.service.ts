import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../config/environment/environment';

export interface AuditLogItem {
  id: string;
  createdAt: string;
  action: string;
  category: string;
  userId?: string | null;
  userName?: string | null;
  entityType?: string | null;
  entityId?: string | null;
  description?: string | null;
  ipAddress?: string | null;
  userAgent?: string | null;
  requestPath?: string | null;
  status: string;
  failureReason?: string | null;
}

export interface CommunicationLogItem {
  id: string;
  createdAt: string;
  channel: string;
  recipientEmail?: string | null;
  recipientMobile?: string | null;
  messageType: string;
  templateName?: string | null;
  subject?: string | null;
  status: string;
  provider?: string | null;
  providerMessageId?: string | null;
  failureReason?: string | null;
  relatedEntityType?: string | null;
  relatedEntityId?: string | null;
  triggeredByUserId?: string | null;
  triggeredByUserName?: string | null;
}

export interface LogQuery {
  dateFrom?: string;
  dateTo?: string;
  userId?: string;
  action?: string;
  category?: string;
  entityType?: string;
  channel?: string;
  status?: string;
  messageType?: string;
  recipient?: string;
  page?: number;
  pageSize?: number;
}

export interface LogPage<T> {
  total: number;
  page: number;
  pageSize: number;
  items: T[];
}

@Injectable({
  providedIn: 'root'
})
export class AuditLogsService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiBaseUrl}/admin/logs`;

  getAuditLogs(query: LogQuery): Observable<LogPage<AuditLogItem>> {
    return this.http.get<LogPage<AuditLogItem>>(`${this.apiUrl}/audit`, {
      params: this.buildParams(query)
    });
  }

  getCommunicationLogs(query: LogQuery): Observable<LogPage<CommunicationLogItem>> {
    return this.http.get<LogPage<CommunicationLogItem>>(`${this.apiUrl}/communication`, {
      params: this.buildParams(query)
    });
  }

  private buildParams(query: LogQuery): HttpParams {
    let params = new HttpParams();

    Object.entries(query).forEach(([key, value]) => {
      if (value !== undefined && value !== null && `${value}`.trim() !== '') {
        params = params.set(key, `${value}`);
      }
    });

    return params;
  }
}
