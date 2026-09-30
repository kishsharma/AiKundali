import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../config/environment';
import { CustomerAddress, SaveAddressRequest } from '../models/store.models';

@Injectable({ providedIn: 'root' })
export class AddressService {
  private readonly baseUrl = `${environment.apiBaseUrl}/store/addresses`;

  constructor(private readonly http: HttpClient) {}

  list(): Observable<CustomerAddress[]> {
    return this.http.get<CustomerAddress[]>(this.baseUrl);
  }

  create(request: SaveAddressRequest): Observable<CustomerAddress> {
    return this.http.post<CustomerAddress>(this.baseUrl, request);
  }
}
