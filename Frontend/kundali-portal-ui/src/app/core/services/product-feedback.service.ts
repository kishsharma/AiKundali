import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../config/environment/environment';

export interface FeedbackTopic {
  key: string;
  label: string;
}

export interface SubmitProductFeedbackRequest {
  surface: string;
  contextKey: string;
  sentiment: 'useful' | 'needs_work';
  topic?: string | null;
  note?: string | null;
}

export interface ProductFeedbackItem {
  id: string;
  fullName: string;
  email?: string | null;
  surface: string;
  contextKey: string;
  sentiment: string;
  topic?: string | null;
  note?: string | null;
  planCode: string;
  createdAt: string;
}

@Injectable({
  providedIn: 'root'
})
export class ProductFeedbackService {
  private readonly apiUrl = `${environment.apiBaseUrl}/feedback`;

  constructor(private http: HttpClient) {}

  prompt(surface: string, contextKey: string): Observable<{ show: boolean }> {
    const params = new HttpParams().set('surface', surface).set('contextKey', contextKey);
    return this.http.get<{ show: boolean }>(`${this.apiUrl}/prompt`, { params });
  }

  submit(request: SubmitProductFeedbackRequest): Observable<{ saved: boolean; alreadyGiven: boolean }> {
    return this.http.post<{ saved: boolean; alreadyGiven: boolean }>(this.apiUrl, request);
  }

  list(): Observable<ProductFeedbackItem[]> {
    return this.http.get<ProductFeedbackItem[]>(this.apiUrl);
  }
}
