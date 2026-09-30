import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { PageHeaderComponent } from '../../shared/components/page-header.component';
import { NotificationService } from '../../shared/services/notification.service';
import { AdminCoupon, AdminPlan, PlanOffersService, SaveCouponPayload } from '../../core/services/plan-offers.service';

@Component({
  selector: 'app-plan-offers',
  standalone: true,
  imports: [CommonModule, FormsModule, PageHeaderComponent],
  templateUrl: './plan-offers.component.html',
  styleUrl: './plan-offers.component.scss'
})
export class PlanOffersComponent implements OnInit {
  private readonly planOffers = inject(PlanOffersService);
  private readonly notifications = inject(NotificationService);
  private readonly cdr = inject(ChangeDetectorRef);

  plans: AdminPlan[] = [];
  coupons: AdminCoupon[] = [];
  loading = true;
  savingPlans = false;
  couponDraft = this.emptyCoupon();

  ngOnInit(): void {
    this.planOffers.getPlans().subscribe({
      next: (plans) => {
        this.plans = plans;
        this.loading = false;
        this.paint();
      },
      error: (error) => {
        this.loading = false;
        this.notifications.error(error?.error?.message || 'Unable to load plans.');
        this.paint();
      }
    });
    this.planOffers.getCoupons().subscribe({
      next: (coupons) => {
        this.coupons = coupons;
        this.paint();
      },
      error: (error) => {
        this.notifications.error(error?.error?.message || 'Unable to load coupons.');
        this.paint();
      }
    });
  }

  rupees(paise: number): number {
    return Math.round(paise) / 100;
  }

  setRupees(plan: AdminPlan, cycle: 'monthly' | 'yearly', value: string): void {
    const paise = Math.round(Number(value || 0) * 100);
    if (cycle === 'monthly') {
      plan.monthlyAmountPaise = paise;
      return;
    }

    plan.yearlyAmountPaise = paise;
  }

  orderedPlans(): AdminPlan[] {
    return ['Free', 'Premium', 'PremiumPlus']
      .map((code) => this.plans.find((plan) => plan.code === code))
      .filter((plan): plan is AdminPlan => !!plan);
  }

  planTitle(plan: AdminPlan): string {
    return plan.code === 'PremiumPlus' ? 'Premium Plus' : plan.code;
  }

  toggleCheckout(plan: AdminPlan): void {
    if (plan.code === 'Free') {
      plan.checkoutOpen = false;
      return;
    }

    plan.checkoutOpen = !plan.checkoutOpen;
  }

  savePlans(): void {
    const plans = this.orderedPlans();
    if (!plans.length) {
      return;
    }

    this.savingPlans = true;
    forkJoin(plans.map((plan) => this.planOffers.updatePlan(plan))).subscribe({
      next: (saved) => {
        this.plans = saved;
        this.savingPlans = false;
        this.notifications.success('Plans saved.');
        this.paint();
      },
      error: (error) => {
        this.savingPlans = false;
        this.notifications.error(error?.error?.message || 'Unable to save the plans.');
        this.paint();
      }
    });
  }

  saveCoupon(): void {
    const payload = this.toPayload(this.couponDraft);
    const request = this.couponDraft.id
      ? this.planOffers.updateCoupon(this.couponDraft.id, payload)
      : this.planOffers.createCoupon(payload);
    request.subscribe({
      next: (saved) => {
        this.coupons = this.couponDraft.id
          ? this.coupons.map((item) => item.id === saved.id ? saved : item)
          : [saved, ...this.coupons];
        this.couponDraft = this.emptyCoupon();
        this.notifications.success(`${saved.code} saved.`);
        this.paint();
      },
      error: (error) => {
        this.notifications.error(error?.error?.message || 'Unable to save the coupon.');
        this.paint();
      }
    });
  }

  editCoupon(coupon: AdminCoupon): void {
    this.couponDraft = {
      id: coupon.id,
      name: coupon.name,
      code: coupon.code,
      percentOff: coupon.percentOff,
      premium: coupon.planCodes.includes('Premium'),
      plus: coupon.planCodes.includes('PremiumPlus'),
      monthly: coupon.billingCycles.includes('monthly'),
      yearly: coupon.billingCycles.includes('yearly'),
      startsAt: this.toLocalInput(coupon.startsAt),
      endsAt: this.toLocalInput(coupon.endsAt),
      maxRedemptions: coupon.maxRedemptions ?? null,
      active: coupon.active
    };
  }

  deactivate(coupon: AdminCoupon): void {
    this.planOffers.updateCoupon(coupon.id, {
      name: coupon.name,
      code: coupon.code,
      percentOff: coupon.percentOff,
      planCodes: coupon.planCodes,
      billingCycles: coupon.billingCycles,
      startsAt: coupon.startsAt,
      endsAt: coupon.endsAt,
      maxRedemptions: coupon.maxRedemptions,
      active: false
    }).subscribe({
      next: (saved) => {
        this.coupons = this.coupons.map((item) => item.id === saved.id ? saved : item);
        this.notifications.success(`${saved.code} is inactive.`);
        this.paint();
      },
      error: (error) => {
        this.notifications.error(error?.error?.message || 'Unable to turn off the coupon.');
        this.paint();
      }
    });
  }

  private paint(): void {
    this.cdr.markForCheck();
  }

  private toPayload(draft: CouponDraft): SaveCouponPayload {
    return {
      name: draft.name,
      code: draft.code,
      percentOff: Number(draft.percentOff),
      planCodes: [draft.premium ? 'Premium' : '', draft.plus ? 'PremiumPlus' : ''].filter(Boolean),
      billingCycles: [draft.monthly ? 'monthly' : '', draft.yearly ? 'yearly' : ''].filter(Boolean),
      startsAt: draft.startsAt ? new Date(draft.startsAt).toISOString() : null,
      endsAt: draft.endsAt ? new Date(draft.endsAt).toISOString() : null,
      maxRedemptions: draft.maxRedemptions ? Number(draft.maxRedemptions) : null,
      active: draft.active
    };
  }

  resetDraft(): void {
    this.couponDraft = this.emptyCoupon();
  }

  private emptyCoupon(): CouponDraft {
    return {
      id: '',
      name: '',
      code: '',
      percentOff: 10,
      premium: true,
      plus: false,
      monthly: true,
      yearly: true,
      startsAt: '',
      endsAt: '',
      maxRedemptions: null,
      active: true
    };
  }

  private toLocalInput(value?: string | null): string {
    if (!value) {
      return '';
    }

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
      return '';
    }

    const pad = (part: number) => String(part).padStart(2, '0');
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
  }
}

interface CouponDraft {
  id: string;
  name: string;
  code: string;
  percentOff: number;
  premium: boolean;
  plus: boolean;
  monthly: boolean;
  yearly: boolean;
  startsAt: string;
  endsAt: string;
  maxRedemptions: number | null;
  active: boolean;
}
