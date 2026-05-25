import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../config/environment/environment';

@Injectable({
  providedIn: 'root'
})
export class KundaliService {

  private apiUrl =
    `${environment.apiBaseUrl}/${environment.apiEndpoints.kundali.generate}`;

  constructor(private http: HttpClient) {}

  generateKundali(payload: any): Observable<any> {
    return this.http.post<any>(this.apiUrl, payload);
  }
 
}