import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../config/environment';
import {
  InventoryAdjustRequest,
  InventoryListItem,
  InventoryReorderLevelRequest,
  InventoryTransactionListItem
} from '../models/inventory.models';

@Injectable({ providedIn: 'root' })
export class InventoryAdminService {
  private readonly baseUrl = `${environment.apiBaseUrl}/store/inventory`;

  constructor(private readonly http: HttpClient) {}

  list(lowStockOnly = false): Observable<InventoryListItem[]> {
    const params = new HttpParams().set('lowStockOnly', String(lowStockOnly));
    return this.http.get<InventoryListItem[]>(this.baseUrl, { params });
  }

  transactions(inventoryId: string): Observable<InventoryTransactionListItem[]> {
    return this.http.get<InventoryTransactionListItem[]>(
      `${this.baseUrl}/${inventoryId}/transactions`
    );
  }

  adjust(inventoryId: string, request: InventoryAdjustRequest): Observable<InventoryListItem> {
    return this.http.post<InventoryListItem>(`${this.baseUrl}/${inventoryId}/adjust`, request);
  }

  setReorderLevel(
    inventoryId: string,
    request: InventoryReorderLevelRequest
  ): Observable<InventoryListItem> {
    return this.http.put<InventoryListItem>(
      `${this.baseUrl}/${inventoryId}/reorder-level`,
      request
    );
  }
}
