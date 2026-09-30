import { NgFor, NgIf } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { BillingPlan, BillingService } from '../../core/services/billing.service';
import { SubscriptionModalService } from '../../core/services/subscription-modal.service';
import { business } from '../../config/business.config';
import { SiteFooterComponent } from '../../shared/components/site-footer/site-footer.component';

@Component({
  selector: 'app-pricing',
  standalone: true,
  imports: [NgFor, NgIf, SiteFooterComponent],
  templateUrl: './pricing.component.html',
  styleUrl: './pricing.component.scss',
})
export class PricingComponent implements OnInit {
  readonly business = business;
  billingCycle: 'monthly' | 'yearly' = 'monthly';
  plans: BillingPlan[] = [];

  private readonly billingService = inject(BillingService);
  private readonly subscriptionModal = inject(SubscriptionModalService);
  private readonly cdr = inject(ChangeDetectorRef);

  ngOnInit(): void {
    this.billingService.getPlans().subscribe({
      next: (plans) => {
        this.plans = plans.length ? plans : this.fallbackPlans();
        this.cdr.markForCheck();
      },
      error: () => {
        this.plans = this.fallbackPlans();
        this.cdr.markForCheck();
      },
    });
  }

  setCycle(cycle: 'monthly' | 'yearly'): void {
    this.billingCycle = cycle;
  }

  priceLabel(plan: BillingPlan): string {
    const paise = this.billingCycle === 'yearly' ? plan.yearlyAmountPaise : plan.monthlyAmountPaise;
    const amount = paise / 100;
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: plan.currency || 'INR',
      maximumFractionDigits: 0,
    }).format(amount);
  }

  cycleLabel(plan: BillingPlan): string {
    if (plan.monthlyAmountPaise <= 0 && plan.yearlyAmountPaise <= 0) {
      return '';
    }
    return this.billingCycle === 'yearly' ? '/ year' : '/ month';
  }

  openCheckout(plan: BillingPlan): void {
    if (plan.monthlyAmountPaise <= 0 && plan.yearlyAmountPaise <= 0) {
      return;
    }
    this.subscriptionModal.open();
  }

  isFree(plan: BillingPlan): boolean {
    return plan.monthlyAmountPaise <= 0 && plan.yearlyAmountPaise <= 0;
  }

  private fallbackPlans(): BillingPlan[] {
    return [
      { code: 'Free', name: 'Free', tagline: 'Try the portal', currency: 'INR', monthlyAmountPaise: 0, yearlyAmountPaise: 0, kundaliSaveLimit: 5, matchSaveLimit: 2, canCheckout: false },
      { code: 'Premium', name: 'Premium', tagline: 'Full personal reports', currency: 'INR', monthlyAmountPaise: 69900, yearlyAmountPaise: 699900, kundaliSaveLimit: 50, matchSaveLimit: 25, canCheckout: true },
      { code: 'PremiumPlus', name: 'Premium Plus', tagline: 'Your brand on client reports', currency: 'INR', monthlyAmountPaise: 99900, yearlyAmountPaise: 999900, kundaliSaveLimit: 500, matchSaveLimit: 200, canCheckout: true },
    ];
  }
}
