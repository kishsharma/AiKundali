import { Component, Input, inject } from '@angular/core';
import { AuthService } from '../../../core/auth/auth.service';
import { planDisplayName, planUpgradeLabel, resolvePlanCode } from '../../../core/auth/plan.util';
import { SubscriptionModalService } from '../../../core/services/subscription-modal.service';

@Component({
  selector: 'app-plan-chip',
  standalone: true,
  template: `
    <button
      type="button"
      class="plan-chip"
      [class.plan-chip--toolbar]="variant === 'toolbar'"
      [class.plan-chip--sidebar]="variant === 'sidebar'"
      [class.plan-chip--compact]="variant === 'compact'"
      [attr.data-plan]="planCode"
      [attr.aria-label]="ariaLabel"
      [title]="ariaLabel"
      (click)="openPlans($event)">
      <span class="plan-dot" aria-hidden="true"></span>
      <span class="plan-copy">
        @if (variant === 'sidebar') {
          <span class="plan-kicker">Current plan</span>
        }
        <span class="plan-name">{{ planLabel }}</span>
      </span>
      <span class="plan-cta">{{ ctaLabel }}</span>
    </button>
  `,
  styles: [`
    :host {
      display: block;
      min-width: 0;
    }

    .plan-chip {
      display: inline-flex;
      align-items: center;
      gap: 7px;
      max-width: 100%;
      min-height: 22px;
      padding: 2px 8px 2px 6px;
      border: 1px solid rgba(241, 184, 59, 0.38);
      border-radius: 999px;
      background: linear-gradient(180deg, rgba(255, 247, 230, 0.16), rgba(241, 184, 59, 0.08));
      color: inherit;
      cursor: pointer;
      font-family: inherit;
    }

    .plan-chip:hover,
    .plan-chip:focus-visible {
      border-color: #f1b83b;
      box-shadow: 0 0 0 3px rgba(241, 184, 59, 0.16);
      outline: none;
    }

    .plan-dot {
      width: 7px;
      height: 7px;
      flex: 0 0 7px;
      border-radius: 999px;
      background: #9aa0b6;
      box-shadow: 0 0 0 3px rgba(154, 160, 182, 0.16);
    }

    .plan-copy {
      display: flex;
      min-width: 0;
      align-items: baseline;
      gap: 6px;
    }

    .plan-kicker {
      color: rgba(255, 247, 230, 0.62);
      font-family: "IBM Plex Mono", monospace;
      font-size: 8px;
      font-weight: 800;
      letter-spacing: 0.12em;
      text-transform: uppercase;
    }

    .plan-name {
      overflow: hidden;
      font-size: 11px;
      font-weight: 800;
      letter-spacing: 0.02em;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .plan-cta {
      margin-left: auto;
      color: #e15f3d;
      font-size: 10px;
      font-weight: 800;
      letter-spacing: 0.04em;
      text-transform: uppercase;
      white-space: nowrap;
    }

    .plan-chip[data-plan='Free'] {
      border-color: rgba(38, 63, 135, 0.28);
      background: rgba(38, 63, 135, 0.08);
    }

    .plan-chip[data-plan='Free'] .plan-dot {
      background: #263f87;
      box-shadow: 0 0 0 3px rgba(38, 63, 135, 0.14);
    }

    .plan-chip[data-plan='Premium'] {
      border-color: #b8862a;
      background: linear-gradient(180deg, #fff8dc 0%, #f6d56a 46%, #e2b23c 100%);
      box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.9), 0 6px 14px rgba(184, 134, 42, 0.28);
    }

    .plan-chip[data-plan='Premium'] .plan-kicker {
      color: #8a5a10;
    }

    .plan-chip[data-plan='Premium'] .plan-name {
      color: #5c3b08;
      font-family: "Cormorant Garamond", Georgia, serif;
      font-size: 15px;
      font-weight: 700;
      letter-spacing: 0.03em;
    }

    .plan-chip[data-plan='Premium'] .plan-dot,
    .plan-chip[data-plan='PremiumPlus'] .plan-dot {
      width: 10px;
      height: 10px;
      flex-basis: 10px;
      border-radius: 0;
      box-shadow: none;
      clip-path: polygon(50% 0%, 62% 35%, 100% 38%, 70% 59%, 79% 96%, 50% 75%, 21% 96%, 30% 59%, 0 38%, 38% 35%);
    }

    .plan-chip[data-plan='Premium'] .plan-dot {
      background: #8a5a10;
    }

    .plan-chip[data-plan='PremiumPlus'] {
      border-color: #8e3b23;
      background: linear-gradient(180deg, #c45532 0%, #8e3b23 55%, #5c2616 100%);
      box-shadow: inset 0 1px 0 rgba(255, 220, 180, 0.45), 0 6px 14px rgba(142, 59, 35, 0.28);
    }

    .plan-chip[data-plan='PremiumPlus'] .plan-kicker {
      color: rgba(255, 246, 232, 0.78);
    }

    .plan-chip[data-plan='PremiumPlus'] .plan-name {
      color: #fff6e8;
      font-family: "Cormorant Garamond", Georgia, serif;
      font-size: 15px;
      font-weight: 700;
      letter-spacing: 0.03em;
    }

    .plan-chip[data-plan='PremiumPlus'] .plan-dot {
      background: #f6d56a;
    }

    .plan-chip[data-plan='PremiumPlus'] .plan-cta {
      color: #fff6e8;
    }

    .plan-chip--toolbar {
      min-height: 36px;
      padding: 4px 12px 4px 8px;
      flex-shrink: 0;
    }

    .plan-chip--sidebar {
      width: 100%;
      min-height: 48px;
      padding: 8px 10px;
      border-radius: 12px;
      gap: 8px;
    }

    .plan-chip--sidebar .plan-copy {
      flex-direction: column;
      gap: 1px;
    }

    .plan-chip--sidebar[data-plan='Premium'] .plan-name,
    .plan-chip--sidebar[data-plan='PremiumPlus'] .plan-name {
      font-size: 18px;
    }

    .plan-chip--compact {
      width: 28px;
      min-height: 28px;
      justify-content: center;
      padding: 0;
    }

    .plan-chip--compact .plan-copy,
    .plan-chip--compact .plan-cta {
      display: none;
    }

    .plan-chip:not(.plan-chip--sidebar):not(.plan-chip--compact) .plan-cta {
      display: none;
    }

    :host-context(body[data-theme='light']) .plan-kicker {
      color: #8e3b23;
    }

    :host-context(body[data-theme='light']) .plan-chip[data-plan='Free'] .plan-name {
      color: #263f87;
    }

    :host-context(body[data-theme='light']) .plan-chip[data-plan='Free']:not(.plan-chip--sidebar) .plan-name {
      color: #263f87;
    }
  `]
})
export class PlanChipComponent {
  private readonly authService = inject(AuthService);
  private readonly subscriptionModal = inject(SubscriptionModalService);

  @Input() variant: 'toolbar' | 'sidebar' | 'compact' = 'toolbar';

  get planCode(): string {
    return resolvePlanCode(this.authService.getCurrentUser());
  }

  get planLabel(): string {
    return planDisplayName(this.authService.getCurrentUser());
  }

  get ctaLabel(): string {
    return planUpgradeLabel(this.authService.getCurrentUser());
  }

  get ariaLabel(): string {
    return `${this.planLabel} plan. ${this.ctaLabel} subscription.`;
  }

  openPlans(event: Event): void {
    event.preventDefault();
    event.stopPropagation();
    this.subscriptionModal.open();
  }
}
