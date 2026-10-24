import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, HostListener, OnDestroy, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Subscription, forkJoin, of, timeout, catchError } from 'rxjs';
import { AuthService } from '../../../core/auth/auth.service';
import { BillingPlan, BillingService, BillingStatus, CheckoutSession, CouponPreview } from '../../../core/services/billing.service';
import { SubscriptionModalService } from '../../../core/services/subscription-modal.service';
import { NotificationService } from '../../services/notification.service';

interface RazorpayCheckoutResponse {
  razorpay_payment_id?: string;
  razorpay_order_id?: string;
  razorpay_signature?: string;
  error?: {
    description?: string;
    reason?: string;
  };
}

declare global {
  interface Window {
    Razorpay?: new (options: Record<string, unknown>) => {
      open: () => void;
      on: (event: string, handler: (response: RazorpayCheckoutResponse) => void) => void;
    };
  }
}

@Component({
  selector: 'app-subscription-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './subscription-modal.component.html',
  styleUrl: './subscription-modal.component.scss'
})
export class SubscriptionModalComponent implements OnDestroy {
  private readonly billingService = inject(BillingService);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly notifications = inject(NotificationService);
  private readonly subscriptionModal = inject(SubscriptionModalService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly openSub: Subscription;

  isOpen = false;
  plans: BillingPlan[] = [];
  billingCycle: 'monthly' | 'yearly' = 'monthly';
  private cycleTouched = false;
  loading = false;
  checkoutPlanCode = '';
  couponCode = '';
  couponBusy = false;
  couponMessage = '';
  couponError = false;
  celebrating = false;
  private couponQuotes: CouponPreview[] = [];
  status: BillingStatus | null = null;
  currentPlanCode = 'Free';
  loaded = false;

  readonly comparison = [
    { label: 'Saved charts', free: '5', premium: '50', plus: '500' },
    { label: 'Saved matches', free: '2', premium: '25', plus: '200' },
    { label: 'Full kundali report', free: 'No', premium: 'Yes', plus: 'Yes' },
    { label: 'Dedicated Dasha page', free: 'Limited', premium: 'Complete', plus: 'Complete' },
    { label: 'Full matching details', free: 'Score only', premium: 'Yes', plus: 'Yes' },
    { label: 'PDF and email', free: 'No', premium: 'Yes', plus: 'Yes' },
    { label: 'White-label branding', free: 'No', premium: 'No', plus: 'Yes' },
    { label: 'Dhruva', free: '2 life questions a day, no AI', premium: 'AI mode and custom questions', plus: 'AI mode and custom questions' }
  ];

  constructor() {
    this.openSub = this.subscriptionModal.isOpen$.subscribe((isOpen) => {
      this.isOpen = isOpen;
      document.body.style.overflow = isOpen ? 'hidden' : '';
      if (isOpen) {
        this.cycleTouched = false;
        this.loadPlans();
      }
      this.paint();
    });
  }

  ngOnDestroy(): void {
    this.openSub.unsubscribe();
    document.body.style.overflow = '';
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.isOpen) {
      this.close();
    }
  }

  close(): void {
    this.subscriptionModal.close();
  }

  onBackdropClick(event: MouseEvent): void {
    if ((event.target as HTMLElement).classList.contains('subscription-backdrop')) {
      this.close();
    }
  }

  setBillingCycle(cycle: 'monthly' | 'yearly'): void {
    this.billingCycle = cycle;
    this.cycleTouched = true;
    this.couponQuotes = [];
    this.couponMessage = '';
    this.couponError = false;
  }

  applyCoupon(): void {
    const code = this.couponCode.trim();
    if (!code) {
      return;
    }

    if (code.toLowerCase() === 'navaratri') {
      this.acceptNavaratri();
      return;
    }

    const targets = this.plans.filter((plan) => !this.isFree(plan));
    if (!targets.length) {
      this.couponError = true;
      this.couponMessage = 'Plans are still loading. Try the code again.';
      this.paint();
      return;
    }

    this.couponBusy = true;
    forkJoin(targets.map((plan) => this.billingService.previewCoupon(code, plan.code, this.billingCycle).pipe(
      catchError((error) => of({ planCode: plan.code, message: error?.error?.message || 'That coupon is not valid.' }))
    ))).subscribe((results) => {
      this.couponBusy = false;
      this.couponQuotes = results.filter((item): item is CouponPreview => 'amountPaise' in item);
      if (!this.couponQuotes.length) {
        this.couponError = true;
        const failed = results.find((item) => 'message' in item) as { message?: string } | undefined;
        this.couponMessage = failed?.message || 'That coupon is not valid.';
        this.paint();
        return;
      }

      this.couponError = false;
      this.couponMessage = this.couponQuotes[0].complimentary
        ? 'Navaratri upgrades Free to Premium for one year. No payment.'
        : `${this.couponQuotes[0].code} takes ${this.couponQuotes[0].percentOff}% off.`;
      this.paint();
    });
  }

  discountedAmount(plan: BillingPlan): string | null {
    const quote = this.couponQuotes.find((item) => item.planCode.toLowerCase() === (plan.code || '').toLowerCase());
    if (!quote) {
      return null;
    }

    if (quote.complimentary || quote.amountPaise === 0) {
      return 'Free';
    }

    return `₹${(quote.amountPaise / 100).toLocaleString('en-IN')}`;
  }

  currentCycleLabel(): string {
    return this.currentCycle === 'yearly' ? 'Yearly' : 'Monthly';
  }

  get isLoggedIn(): boolean {
    return this.authService.isLoggedIn();
  }

  priceLabel(plan: BillingPlan): string {
    if (this.isFree(plan)) {
      return 'Free';
    }

    return `${this.priceAmount(plan)}${this.priceCycle}`;
  }

  priceAmount(plan: BillingPlan): string {
    const amount = this.billingCycle === 'yearly' ? plan.yearlyAmountPaise : plan.monthlyAmountPaise;
    if (!amount) {
      return 'Free';
    }

    return `₹${(amount / 100).toLocaleString('en-IN')}`;
  }

  get priceCycle(): string {
    return this.billingCycle === 'yearly' ? '/yr' : '/mo';
  }

  isFree(plan: BillingPlan): boolean {
    return (plan.code || 'Free').toLowerCase() === 'free';
  }

  isFeatured(plan: BillingPlan): boolean {
    return (plan.code || '').toLowerCase() === 'premium';
  }

  featureTone(value: string): 'yes' | 'no' | 'mixed' {
    const normalized = (value || '').trim().toLowerCase();
    if (normalized === 'yes' || normalized === 'complete') {
      return 'yes';
    }
    if (normalized === 'no') {
      return 'no';
    }
    return 'mixed';
  }

  get currentCycle(): 'monthly' | 'yearly' | null {
    const status = String(this.status?.subscriptionStatus || '').trim().toLowerCase();
    const paid = status === 'active' || status === 'authenticated' || status === 'charged' || status === 'cancelling';
    if (!paid) {
      return null;
    }

    const cycle = String(this.status?.billingCycle || '').trim().toLowerCase();
    return cycle === 'yearly' || cycle === 'monthly' ? cycle : null;
  }

  isSamePlan(plan: BillingPlan): boolean {
    return (this.currentPlanCode || 'Free').toLowerCase() === (plan.code || '').toLowerCase();
  }

  isCurrent(plan: BillingPlan): boolean {
    if (!this.isLoggedIn || !this.isSamePlan(plan)) {
      return false;
    }

    if (this.isFree(plan) || !this.currentCycle) {
      return true;
    }

    return this.currentCycle === this.billingCycle;
  }

  isCtaDisabled(plan: BillingPlan): boolean {
    return this.checkoutPlanCode === plan.code || this.isCurrent(plan);
  }

  ctaLabel(plan: BillingPlan): string {
    if (plan.code === 'Free') {
      return this.isLoggedIn ? 'Current starter plan' : 'Create free account';
    }

    if (this.isCurrent(plan)) {
      return this.currentCycle === 'yearly' ? 'Current yearly plan' : 'Current monthly plan';
    }

    if (this.isSamePlan(plan) && this.currentCycle && this.currentCycle !== this.billingCycle) {
      return this.billingCycle === 'yearly' ? 'Switch to yearly' : 'Switch to monthly';
    }

    if (this.complimentaryQuote(plan)) {
      return 'Claim Premium';
    }

    return this.isLoggedIn ? 'Choose plan' : 'Login to upgrade';
  }

  complimentaryQuote(plan: BillingPlan): CouponPreview | null {
    const quote = this.couponQuotes.find((item) => item.planCode.toLowerCase() === (plan.code || '').toLowerCase());
    return quote?.complimentary || quote?.amountPaise === 0 ? quote ?? null : null;
  }

  private acceptNavaratri(): void {
    const premium = this.plans.find((plan) => (plan.code || '').toLowerCase() === 'premium');
    this.billingCycle = 'yearly';
    this.couponCode = 'Navaratri';
    this.couponBusy = false;
    this.couponError = false;
    this.couponQuotes = [{
      code: 'Navaratri',
      name: 'Navaratri gift',
      percentOff: 100,
      planCode: premium?.code || 'Premium',
      billingCycle: 'yearly',
      originalAmountPaise: premium?.yearlyAmountPaise ?? 0,
      amountPaise: 0,
      complimentary: true
    }];
    this.couponMessage = 'Navaratri upgrades Free to Premium for one year. Choose Claim Premium. No payment.';
    this.paint();
  }

  dismissCelebration(): void {
    this.celebrating = false;
    this.close();
  }

  featureFor(plan: BillingPlan, row: { label: string; free: string; premium: string; plus: string }): string {
    if (row.label === 'Saved charts') {
      return String(plan.kundaliSaveLimit);
    }

    if (row.label === 'Saved matches') {
      return String(plan.matchSaveLimit);
    }

    const code = (plan.code || 'Free').toLowerCase();
    if (code === 'premiumplus' || code === 'plus') {
      return row.plus;
    }

    if (code === 'premium') {
      return row.premium;
    }

    return row.free;
  }

  async choosePlan(plan: BillingPlan): Promise<void> {
    if (plan.code === 'Free') {
      if (!this.isLoggedIn) {
        this.close();
        await this.router.navigate(['/register']);
      }
      return;
    }

    if (!this.isLoggedIn) {
      this.close();
      await this.router.navigate(['/login'], { queryParams: { returnUrl: this.router.url } });
      return;
    }

    if (this.isCtaDisabled(plan)) {
      return;
    }

    this.checkoutPlanCode = plan.code;
    if (this.couponCode.trim().toLowerCase() === 'navaratri' && (plan.code || '').toLowerCase() === 'premium') {
      this.acceptNavaratri();
    }
    const gift = this.complimentaryQuote(plan);
    if (!plan.canCheckout || gift) {
      this.billingService.activatePlan(plan.code, gift ? 'yearly' : this.billingCycle, gift?.code).subscribe({
        next: (status) => {
          this.authService.getMe().subscribe({
            next: (profile) => {
              this.currentPlanCode = profile.planCode || status.planCode || plan.code;
              this.status = status;
              this.checkoutPlanCode = '';
              if (gift) {
                this.celebrating = true;
              } else {
                this.notifications.success(`${plan.name} is now active.`);
                this.close();
              }
              this.paint();
            },
            error: () => {
              this.currentPlanCode = status.planCode || plan.code;
              this.status = status;
              this.checkoutPlanCode = '';
              if (gift) {
                this.celebrating = true;
              } else {
                this.notifications.success(`${plan.name} is now active.`);
                this.close();
              }
              this.paint();
            }
          });
        },
        error: (error) => {
          this.checkoutPlanCode = '';
          this.paint();
          this.notifications.error(error?.error?.message || 'Unable to update the plan.');
        }
      });
      return;
    }

    const quote = this.couponQuotes.find((item) => item.planCode.toLowerCase() === plan.code.toLowerCase());
    this.billingService.createCheckout(plan.code, this.billingCycle, quote?.code).subscribe({
      next: async (session) => {
        try {
          await this.openRazorpay(session);
        } catch {
          this.notifications.error('Unable to open checkout.');
          this.checkoutPlanCode = '';
          this.paint();
        }
      },
      error: (error) => {
        this.checkoutPlanCode = '';
        this.paint();
        const status = error?.status;
        const apiMessage = error?.error?.message || error?.error?.detail || error?.error?.title;
        if (status === 0) {
          this.notifications.error('Cannot reach the local API. Start KundaliEngine.API on https://localhost:7245.');
          return;
        }
        if (status === 401) {
          this.notifications.error(apiMessage || 'Log in again, then retry checkout.');
          return;
        }
        if (status === 404) {
          this.notifications.error('Checkout is missing on this API. Point the UI at the local API or deploy billing.');
          return;
        }
        this.notifications.error(apiMessage || 'Checkout is not available yet.');
      }
    });
  }

  cancelSubscription(): void {
    this.billingService.cancel().subscribe({
      next: (response) => this.notifications.success(response.message),
      error: (error) => this.notifications.error(error?.error?.message || 'Unable to cancel the plan.')
    });
  }

  private paint(): void {
    this.cdr.markForCheck();
  }

  private loadPlans(): void {
    this.currentPlanCode = this.authService.getCurrentUser()?.planCode || 'Free';
    this.loading = true;
    this.billingService.getPlans().pipe(
      timeout(8000),
      catchError(() => of(this.fallbackPlans()))
    ).subscribe((plans) => {
      this.plans = Array.isArray(plans) && plans.length ? plans : this.fallbackPlans();
      this.loading = false;
      this.loaded = true;
      this.paint();
    });

    if (this.authService.isLoggedIn()) {
      this.billingService.getStatus().subscribe({
        next: (status) => {
          this.status = status;
          this.currentPlanCode = status.planCode;
          const cycle = String(status.billingCycle || '').trim().toLowerCase();
          if (!this.cycleTouched && (cycle === 'yearly' || cycle === 'monthly')) {
            this.billingCycle = cycle;
          }
          this.paint();
        }
      });
    }
  }

  private async openRazorpay(session: CheckoutSession): Promise<void> {
    await this.loadRazorpay();
    if (!window.Razorpay) {
      throw new Error('Razorpay missing');
    }

    const currentUser = this.authService.getCurrentUser();
    const checkout = new window.Razorpay({
      key: session.keyId,
      name: 'HoraLife',
      description: session.description,
      currency: session.currency,
      amount: session.amountPaise,
      subscription_id: session.subscriptionId || undefined,
      order_id: session.orderId || undefined,
      prefill: {
        name: currentUser?.fullName || '',
        email: currentUser?.email || '',
        contact: currentUser?.phoneNumber || ''
      },
      handler: (response: RazorpayCheckoutResponse) => {
        this.billingService.verifyPayment({
          razorpay_payment_id: response.razorpay_payment_id || '',
          razorpay_order_id: response.razorpay_order_id || session.orderId || '',
          razorpay_signature: response.razorpay_signature || ''
        }).subscribe({
          next: (result) => {
            this.authService.getMe().subscribe({
              next: (profile) => {
                this.currentPlanCode = profile.planCode || result.planCode || session.planCode;
                this.notifications.success(result.message || `${session.planName} is now active.`);
                this.checkoutPlanCode = '';
                this.close();
                this.paint();
              },
              error: () => {
                this.notifications.success(result.message || 'Payment verified.');
                this.checkoutPlanCode = '';
                this.close();
                this.paint();
              }
            });
          },
          error: (error) => {
            this.checkoutPlanCode = '';
            this.notifications.error(error?.error?.message || 'Payment could not be verified.');
            this.paint();
          }
        });
      },
      modal: {
        ondismiss: () => {
          this.checkoutPlanCode = '';
          this.notifications.info('Payment was cancelled.');
          this.paint();
        }
      }
    });
    checkout.on('payment.failed', (response) => {
      this.checkoutPlanCode = '';
      this.notifications.error(response?.error?.description || 'Payment failed. Please try again.');
      this.paint();
    });
    checkout.open();
  }

  private loadRazorpay(): Promise<void> {
    if (window.Razorpay) {
      return Promise.resolve();
    }

    return new Promise((resolve, reject) => {
      const script = document.createElement('script');
      script.src = 'https://checkout.razorpay.com/v1/checkout.js';
      script.onload = () => resolve();
      script.onerror = () => reject(new Error('Failed to load Razorpay'));
      document.body.appendChild(script);
    });
  }

  private fallbackPlans(): BillingPlan[] {
    return [
      { code: 'Free', name: 'Free', tagline: 'Try the portal', currency: 'INR', monthlyAmountPaise: 0, yearlyAmountPaise: 0, kundaliSaveLimit: 5, matchSaveLimit: 2, canCheckout: false },
      { code: 'Premium', name: 'Premium', tagline: 'Full personal reports', currency: 'INR', monthlyAmountPaise: 69900, yearlyAmountPaise: 699900, kundaliSaveLimit: 50, matchSaveLimit: 25, canCheckout: false },
      { code: 'PremiumPlus', name: 'Premium Plus', tagline: 'Your brand on client reports', currency: 'INR', monthlyAmountPaise: 99900, yearlyAmountPaise: 999900, kundaliSaveLimit: 500, matchSaveLimit: 200, canCheckout: false }
    ];
  }
}
