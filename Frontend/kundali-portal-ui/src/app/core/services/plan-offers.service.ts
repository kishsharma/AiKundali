import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../config/environment/environment';

export interface AdminPlan {
  code: string;
  name: string;
  tagline: string;
  monthlyAmountPaise: number;
  yearlyAmountPaise: number;
  kundaliSaveLimit: number;
  matchSaveLimit: number;
  aiChatEnabled: boolean;
  lifeQuestionLimit: number | null;
  checkoutOpen: boolean;
  updatedAt?: string;
}

export interface AdminCoupon {
  id: string;
  name: string;
  code: string;
  percentOff: number;
  planCodes: string[];
  billingCycles: string[];
  startsAt?: string | null;
  endsAt?: string | null;
  maxRedemptions?: number | null;
  redeemedCount: number;
  active: boolean;
}

export interface SaveCouponPayload {
  name: string;
  code: string;
  percentOff: number;
  planCodes: string[];
  billingCycles: string[];
  startsAt?: string | null;
  endsAt?: string | null;
  maxRedemptions?: number | null;
  active: boolean;
}

@Injectable({ providedIn: 'root' })
export class PlanOffersService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiBaseUrl}/billing/admin`;

  private limitOrUnlimited(value: number | null | undefined): number | null {
    if (value === null || value === undefined || Number.isNaN(Number(value))) {
      return null;
    }

    return Math.max(0, Math.trunc(Number(value)));
  }

  getPlans(): Observable<AdminPlan[]> {
    return this.http.get<AdminPlan[]>(`${this.apiUrl}/plans`);
  }

  updatePlan(plan: AdminPlan): Observable<AdminPlan> {
    return this.http.put<AdminPlan>(`${this.apiUrl}/plans/${plan.code}`, {
      name: plan.name,
      tagline: plan.tagline,
      monthlyAmountPaise: plan.monthlyAmountPaise,
      yearlyAmountPaise: plan.yearlyAmountPaise,
      kundaliSaveLimit: plan.kundaliSaveLimit,
      matchSaveLimit: plan.matchSaveLimit,
      aiChatEnabled: !!plan.aiChatEnabled,
      lifeQuestionLimit: this.limitOrUnlimited(plan.lifeQuestionLimit),
      checkoutOpen: plan.checkoutOpen
    });
  }

  getCoupons(): Observable<AdminCoupon[]> {
    return this.http.get<AdminCoupon[]>(`${this.apiUrl}/coupons`);
  }

  createCoupon(payload: SaveCouponPayload): Observable<AdminCoupon> {
    return this.http.post<AdminCoupon>(`${this.apiUrl}/coupons`, payload);
  }

  updateCoupon(id: string, payload: SaveCouponPayload): Observable<AdminCoupon> {
    return this.http.put<AdminCoupon>(`${this.apiUrl}/coupons/${id}`, payload);
  }
}
