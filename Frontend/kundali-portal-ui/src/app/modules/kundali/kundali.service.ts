import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, Subject, timeout } from 'rxjs';
import { tap } from 'rxjs/operators';

import { apiEndpoints } from '../../config/api-endpoints';
import { environment } from '../../config/environment/environment';

@Injectable({
  providedIn: 'root'
})
export class KundaliService {

  private kundaliBaseUrl = `${environment.apiBaseUrl}/${apiEndpoints.kundali.root}`;
  private generateUrl = `${environment.apiBaseUrl}/${apiEndpoints.kundali.generate}`;
  private saveUrl = `${environment.apiBaseUrl}/${apiEndpoints.kundali.save}`;
  private reportShareUrl = `${environment.apiBaseUrl}/${apiEndpoints.reportShare.sendEmail}`;
  private reportPdfUrl = `${environment.apiBaseUrl}/${apiEndpoints.reportShare.pdf}`;
  private savedKundaliChangedSubject = new Subject<void>();
  savedKundaliChanged$ = this.savedKundaliChangedSubject.asObservable();

  constructor(private http: HttpClient) {}

  generateKundali(payload: any): Observable<any> {
    return this.http.post<any>(this.generateUrl, payload);
  }

  saveKundali(payload: any): Observable<any> {
    return this.http.post<any>(this.saveUrl, payload).pipe(
      tap(() => this.notifySavedKundalisChanged())
    );
  }

  sendReportEmail(payload: any): Observable<any> {
    return this.http.post<any>(this.reportShareUrl, payload).pipe(
      timeout(60000)
    );
  }

  generateReportPdf(payload: any): Observable<Blob> {
    return this.http.post(this.reportPdfUrl, payload, { responseType: 'blob' }).pipe(
      timeout(60000)
    );
  }

  getSavedKundalis(): Observable<any[]> {
    return this.http.get<any[]>(this.kundaliBaseUrl);
  }

  getSavedKundali(id: string): Observable<any> {
    return this.http.get<any>(`${this.kundaliBaseUrl}/${id}`);
  }

  deleteSavedKundali(id: string): Observable<void> {
    return this.http.delete<void>(`${this.kundaliBaseUrl}/${id}`).pipe(
      timeout(20000),
      tap(() => this.notifySavedKundalisChanged())
    );
  }

  notifySavedKundalisChanged(): void {
    this.savedKundaliChangedSubject.next();
  }
}
