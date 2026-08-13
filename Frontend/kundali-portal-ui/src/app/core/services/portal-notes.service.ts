import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, timeout } from 'rxjs';
import { environment } from '../../config/environment/environment';

export interface PortalNote {
  id?: string | null;
  contextType: string;
  contextId?: string | null;
  content: string;
  contextTitle?: string | null;
  contextDetails?: string[] | null;
  voiceRecordingCount?: number;
  createdAt?: string | null;
  updatedAt?: string | null;
}

export interface UpsertPortalNoteRequest {
  contextType: string;
  contextId?: string | null;
  content: string;
  contextTitle?: string | null;
  contextDetails?: string[] | null;
}

export interface PortalNoteVoiceOptions {
  maxDurationSeconds: number;
  maxFileBytes: number;
}

export interface PortalNoteVoiceRecording {
  id: string;
  contextType: string;
  contextId?: string | null;
  fileName: string;
  contentType: string;
  durationSeconds?: number | null;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class PortalNotesService {
  private readonly notesUrl = `${environment.apiBaseUrl}/PortalNotes`;

  constructor(private readonly http: HttpClient) {}

  getNote(contextType: string, contextId?: string | null): Observable<PortalNote> {
    return this.http.get<PortalNote>(this.notesUrl, { params: this.contextParams(contextType, contextId) }).pipe(timeout(20000));
  }

  getAllNotes(): Observable<PortalNote[]> {
    return this.http.get<PortalNote[]>(`${this.notesUrl}/all`).pipe(timeout(20000));
  }

  saveNote(request: UpsertPortalNoteRequest): Observable<PortalNote> {
    return this.http.put<PortalNote>(this.notesUrl, request).pipe(timeout(20000));
  }

  deleteNote(contextType: string, contextId?: string | null): Observable<void> {
    return this.http.delete<void>(this.notesUrl, { params: this.contextParams(contextType, contextId) }).pipe(timeout(20000));
  }

  getVoiceOptions(): Observable<PortalNoteVoiceOptions> {
    return this.http.get<PortalNoteVoiceOptions>(`${this.notesUrl}/voice/options`).pipe(timeout(20000));
  }

  getVoiceRecordings(contextType: string, contextId?: string | null): Observable<PortalNoteVoiceRecording[]> {
    return this.http.get<PortalNoteVoiceRecording[]>(`${this.notesUrl}/voice`, { params: this.contextParams(contextType, contextId) }).pipe(timeout(20000));
  }

  uploadVoiceRecording(contextType: string, contextId: string | null | undefined, blob: Blob, durationSeconds: number, fileName: string): Observable<PortalNoteVoiceRecording> {
    const formData = new FormData();
    formData.append('contextType', contextType);
    if (contextId) {
      formData.append('contextId', contextId);
    }
    formData.append('durationSeconds', String(Math.ceil(durationSeconds)));
    formData.append('file', blob, fileName);

    return this.http.post<PortalNoteVoiceRecording>(`${this.notesUrl}/voice`, formData).pipe(timeout(60000));
  }

  downloadVoiceRecording(id: string): Observable<Blob> {
    return this.http.get(`${this.notesUrl}/voice/${id}`, { responseType: 'blob' }).pipe(timeout(60000));
  }

  deleteVoiceRecording(id: string): Observable<void> {
    return this.http.delete<void>(`${this.notesUrl}/voice/${id}`).pipe(timeout(20000));
  }

  private contextParams(contextType: string, contextId?: string | null): HttpParams {
    let params = new HttpParams().set('contextType', contextType);
    if (contextId) {
      params = params.set('contextId', contextId);
    }

    return params;
  }
}