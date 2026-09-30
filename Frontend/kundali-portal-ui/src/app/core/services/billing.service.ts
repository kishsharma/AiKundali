import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../config/environment/environment';

export interface BillingPlan {
  code: string;
  name: string;
  tagline: string;
  currency: string;
  monthlyAmountPaise: number;
  yearlyAmountPaise: number;
  kundaliSaveLimit: number;
  matchSaveLimit: number;
  canCheckout: boolean;
}

export interface CheckoutSession {
  keyId: string;
  planCode: string;
  planName: string;
  billingCycle: 'monthly' | 'yearly' | string;
  currency: string;
  amountPaise: number;
  subscriptionId?: string | null;
  orderId?: string | null;
  description: string;
}

export interface CouponPreview {
  code: string;
  name: string;
  percentOff: number;
  planCode: string;
  billingCycle: string;
  originalAmountPaise: number;
  amountPaise: number;
}

export interface BillingStatus {
  planCode: string;
  isPaid: boolean;
  isPlus: boolean;
  planExpiresAt?: string | null;
  subscriptionStatus?: string | null;
  billingCycle?: string | null;
  canCancel: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class BillingService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiBaseUrl}/billing`;

  getPlans(): Observable<BillingPlan[]> {
    return this.http.get<BillingPlan[]>(`${this.apiUrl}/plans`);
  }

  getStatus(): Observable<BillingStatus> {
    return this.http.get<BillingStatus>(`${this.apiUrl}/status`);
  }

  createCheckout(planCode: string, billingCycle: 'monthly' | 'yearly', couponCode?: string): Observable<CheckoutSession> {
    return this.http.post<CheckoutSession>(`${this.apiUrl}/checkout`, {
      planCode,
      billingCycle,
      couponCode: couponCode || null
    });
  }

  previewCoupon(code: string, planCode: string, billingCycle: 'monthly' | 'yearly'): Observable<CouponPreview> {
    return this.http.post<CouponPreview>(`${this.apiUrl}/coupons/preview`, { code, planCode, billingCycle });
  }

  verifyPayment(payload: {
    razorpay_payment_id: string;
    razorpay_order_id: string;
    razorpay_signature: string;
  }): Observable<{ verified: boolean; planActivated: boolean; planCode?: string | null; message: string }> {
    return this.http.post<{ verified: boolean; planActivated: boolean; planCode?: string | null; message: string }>(
      `${this.apiUrl}/verify-payment`,
      payload
    );
  }

  cancel(): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/cancel`, {});
  }
}
