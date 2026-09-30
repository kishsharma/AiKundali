import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../config/environment';
import { Cart } from '../models/store.models';

@Injectable({ providedIn: 'root' })
export class CartService {
  private readonly baseUrl = `${environment.apiBaseUrl}/store/cart`;

  constructor(private readonly http: HttpClient) {}

  getCart(): Observable<Cart> {
    return this.http.get<Cart>(this.baseUrl);
  }

  addItem(productId: string, quantity: number, variantId?: string | null): Observable<Cart> {
    return this.http.post<Cart>(`${this.baseUrl}/items`, {
      productId,
      variantId: variantId ?? null,
      quantity
    });
  }

  updateQuantity(cartItemId: string, quantity: number): Observable<Cart> {
    return this.http.put<Cart>(`${this.baseUrl}/items/${cartItemId}`, { quantity });
  }

  removeItem(cartItemId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/items/${cartItemId}`);
  }

  clear(): Observable<void> {
    return this.http.delete<void>(this.baseUrl);
  }
}
