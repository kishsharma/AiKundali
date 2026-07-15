import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../config/environment/environment';

@Injectable({
  providedIn: 'root'
})
export class KundaliService {

  private kundaliBaseUrl = `${environment.apiBaseUrl}/Kundali`;
  private generateUrl = `${environment.apiBaseUrl}/${environment.apiEndpoints.kundali.generate}`;
  private saveUrl = `${environment.apiBaseUrl}/${environment.apiEndpoints.kundali.save}`;

  constructor(private http: HttpClient) {}

  generateKundali(payload: any): Observable<any> {
    return this.http.post<any>(this.generateUrl, payload);
  }

  saveKundali(payload: any): Observable<any> {
    return this.http.post<any>(this.saveUrl, payload);
  }

  getSavedKundalis(): Observable<any[]> {
    return this.http.get<any[]>(this.kundaliBaseUrl);
  }

  getSavedKundali(id: string): Observable<any> {
    return this.http.get<any>(`${this.kundaliBaseUrl}/${id}`);
  }
}
