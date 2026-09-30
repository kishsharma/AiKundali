import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, catchError, map, switchMap, throwError } from 'rxjs';
import { environment } from '../../config/environment';
import {
  CheckoutPreview,
  CheckoutRevalidation,
  ConvertedCart,
  CreatedOrder,
  PlacedOrder
} from '../models/store.models';

export class RevalidationBlockedError extends Error {
  constructor(readonly result: CheckoutRevalidation) {
    super(result.issues[0]?.message || 'Checkout needs another review before the order can be placed.');
  }
}

export interface PlaceOrderRequest {
  shippingAddressId: string;
  useShippingAsBilling: boolean;
  notes?: string;
}

@Injectable({ providedIn: 'root' })
export class CheckoutService {
  private readonly baseUrl = `${environment.apiBaseUrl}/store`;

  constructor(private readonly http: HttpClient) {}

  preview(shippingAddressId: string): Observable<CheckoutPreview> {
    return this.http.post<CheckoutPreview>(`${this.baseUrl}/checkout/preview`, {
      shippingAddressId,
      useShippingAsBilling: true
    });
  }

  placeOrder(request: PlaceOrderRequest): Observable<PlacedOrder> {
    return this.http.post<CheckoutRevalidation>(`${this.baseUrl}/checkout/revalidate`, {}).pipe(
      catchError((error: unknown) => {
        if (error instanceof HttpErrorResponse && error.status === 409 && isRevalidation(error.error)) {
          return throwError(() => new RevalidationBlockedError(error.error as CheckoutRevalidation));
        }
        return throwError(() => error);
      }),
      switchMap((revalidation) => {
        if (!revalidation.isValid) {
          return throwError(() => new RevalidationBlockedError(revalidation));
        }

        return this.http.post<CreatedOrder>(`${this.baseUrl}/orders`, {
          shippingAddressId: request.shippingAddressId,
          useShippingAsBilling: request.useShippingAsBilling,
          notes: request.notes || null
        });
      }),
      switchMap((order) =>
        this.http.post(`${this.baseUrl}/orders/${order.orderId}/items`, {}).pipe(map(() => order))
      ),
      switchMap((order) =>
        this.http.post(`${this.baseUrl}/orders/${order.orderId}/inventory/reserve`, {}).pipe(map(() => order))
      ),
      switchMap((order) =>
        this.http.post<ConvertedCart>(`${this.baseUrl}/orders/${order.orderId}/cart/convert`, {}).pipe(
          map((converted) => ({ order, converted }))
        )
      )
    );
  }
}

function isRevalidation(value: unknown): value is CheckoutRevalidation {
  return !!value && typeof value === 'object' && 'isValid' in value && 'issues' in value;
}
