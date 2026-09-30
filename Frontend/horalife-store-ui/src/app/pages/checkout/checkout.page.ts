import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { readApiError } from '../../core/api/api-error';
import { formatMoney } from '../../core/format';
import { CheckoutPreview, CustomerAddress, SaveAddressRequest } from '../../core/models/store.models';
import { AddressService } from '../../core/services/address.service';
import { CartStateService } from '../../core/services/cart-state.service';
import { CheckoutService, RevalidationBlockedError } from '../../core/services/checkout.service';

const LAST_ORDER_KEY = 'horalife_store_last_order';

@Component({
  selector: 'app-checkout',
  imports: [FormsModule, RouterLink],
  template: `
    <main class="page">
      <h1>Checkout</h1>
      <p class="muted">Delivery is available within India. Payment stays pending until a store payment step is added.</p>
      @if (errorMessage) { <div class="error">{{ errorMessage }}</div> }

      <div class="split">
        <section class="stack">
          <div class="panel">
            <h2>Shipping address</h2>
            @for (address of shippingAddresses; track address.id) {
              <label class="address-card" style="display: block; margin-bottom: 8px;">
                <input type="radio" name="address" [value]="address.id" [(ngModel)]="selectedAddressId" (ngModelChange)="preview = null">
                <strong>{{ address.fullName }}</strong>
                <div>{{ address.addressLine1 }}@if (address.addressLine2) {, {{ address.addressLine2 }}}</div>
                <div>{{ address.city }}, {{ address.state }} {{ address.postalCode }}</div>
                <div class="muted">{{ address.phone }} · {{ address.countryCode }}</div>
              </label>
            }

            <button class="btn-secondary" type="button" (click)="showForm = !showForm">
              {{ showForm ? 'Hide form' : 'Add Indian shipping address' }}
            </button>

            @if (showForm) {
              <form (ngSubmit)="saveAddress()" style="margin-top: 12px;">
                <label class="field"><span>Full name</span><input name="fullName" [(ngModel)]="form.fullName" required></label>
                <label class="field"><span>Phone</span><input name="phone" [(ngModel)]="form.phone" required></label>
                <label class="field"><span>Address line 1</span><input name="line1" [(ngModel)]="form.addressLine1" required></label>
                <label class="field"><span>Address line 2</span><input name="line2" [(ngModel)]="form.addressLine2"></label>
                <label class="field"><span>City</span><input name="city" [(ngModel)]="form.city" required></label>
                <label class="field"><span>State</span><input name="state" [(ngModel)]="form.state" required></label>
                <label class="field"><span>Postal code</span><input name="postalCode" [(ngModel)]="form.postalCode" required></label>
                <label class="field"><span>Country</span><input name="country" [(ngModel)]="form.countryCode" maxlength="2" required></label>
                <label><input type="checkbox" name="isDefault" [(ngModel)]="form.isDefault"> Default shipping address</label>
                <p><button class="btn" type="submit" [disabled]="savingAddress">Save address</button></p>
              </form>
            }
          </div>

          <div class="panel">
            <label class="field"><span>Order notes</span><textarea name="notes" rows="3" [(ngModel)]="notes"></textarea></label>
            <button class="btn" type="button" [disabled]="!selectedAddressId || previewing" (click)="loadPreview()">
              {{ previewing ? 'Reviewing...' : 'Review order' }}
            </button>
          </div>
        </section>

        <aside class="panel">
          <h2>Preview</h2>
          @if (!preview) {
            <p class="muted">Choose an address and review the order before placing it.</p>
          } @else {
            @for (item of preview.items; track item.cartItemId) {
              <div class="row-between">
                <span>{{ item.productName }} @if (item.variantName) { · {{ item.variantName }} } × {{ item.quantity }}</span>
                <span>{{ money(item.lineTotal, preview.currency) }}</span>
              </div>
            }
            <hr>
            <div class="row-between"><span>Subtotal</span><span>{{ money(preview.subtotal, preview.currency) }}</span></div>
            <div class="row-between"><span>Shipping</span><span>{{ money(preview.shippingCharge, preview.currency) }}</span></div>
            <div class="row-between"><strong>Total</strong><strong class="price">{{ money(preview.grandTotal, preview.currency) }}</strong></div>
            <p class="muted">Estimated delivery {{ preview.estimatedDeliveryMinDays }}–{{ preview.estimatedDeliveryMaxDays }} days.</p>
            @for (warning of preview.warnings; track warning) {
              <div class="warning">{{ warning }}</div>
            }
            <button class="btn" type="button" [disabled]="!preview.canCheckout || placing" (click)="placeOrder()">
              {{ placing ? 'Placing order...' : 'Place order' }}
            </button>
            @if (!preview.canCheckout) {
              <p class="muted">This cart cannot be checked out until every line is available.</p>
            }
          }
          <p><a routerLink="/cart">Return to cart</a></p>
        </aside>
      </div>
    </main>
  `
})
export class CheckoutPage implements OnInit {
  shippingAddresses: CustomerAddress[] = [];
  selectedAddressId = '';
  preview: CheckoutPreview | null = null;
  notes = '';
  showForm = false;
  savingAddress = false;
  previewing = false;
  placing = false;
  errorMessage = '';
  form: SaveAddressRequest = {
    addressType: 'shipping',
    fullName: '',
    phone: '',
    addressLine1: '',
    addressLine2: '',
    city: '',
    state: '',
    postalCode: '',
    countryCode: 'IN',
    isDefault: true
  };

  constructor(
    private readonly addresses: AddressService,
    private readonly checkout: CheckoutService,
    private readonly cartState: CartStateService,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    this.loadAddresses();
  }

  money(amount: number, currency: string): string {
    return formatMoney(amount, currency);
  }

  saveAddress(): void {
    this.savingAddress = true;
    this.errorMessage = '';
    this.addresses.create({
      ...this.form,
      countryCode: this.form.countryCode.trim().toUpperCase(),
      addressLine2: this.form.addressLine2?.trim() || undefined
    }).subscribe({
      next: (address) => {
        this.savingAddress = false;
        this.showForm = false;
        this.selectedAddressId = address.id;
        this.preview = null;
        this.loadAddresses();
      },
      error: (error: unknown) => {
        this.savingAddress = false;
        this.errorMessage = readApiError(error, 'The address could not be saved.');
      }
    });
  }

  loadPreview(): void {
    if (!this.selectedAddressId) {
      return;
    }

    this.previewing = true;
    this.errorMessage = '';
    this.checkout.preview(this.selectedAddressId).subscribe({
      next: (preview) => {
        this.previewing = false;
        this.preview = preview;
      },
      error: (error: unknown) => {
        this.previewing = false;
        this.preview = null;
        this.errorMessage = readApiError(error, 'Checkout preview failed.');
      }
    });
  }

  placeOrder(): void {
    if (!this.preview?.canCheckout || !this.selectedAddressId) {
      return;
    }

    this.placing = true;
    this.errorMessage = '';
    this.checkout.placeOrder({
      shippingAddressId: this.selectedAddressId,
      useShippingAsBilling: true,
      notes: this.notes.trim()
    }).subscribe({
      next: (placed) => {
        this.placing = false;
        sessionStorage.setItem(LAST_ORDER_KEY, JSON.stringify(placed));
        this.cartState.refresh();
        void this.router.navigate(['/order-received']);
      },
      error: (error: unknown) => {
        this.placing = false;
        if (error instanceof RevalidationBlockedError) {
          const issues = error.result.issues.map((issue) => issue.message).join(' ');
          this.errorMessage = issues || error.message;
          this.loadPreview();
          return;
        }
        this.errorMessage = readApiError(error, 'The order could not be placed.');
      }
    });
  }

  private loadAddresses(): void {
    this.addresses.list().subscribe({
      next: (addresses) => {
        this.shippingAddresses = addresses.filter((address) => address.addressType === 'shipping');
        if (!this.selectedAddressId) {
          const preferred = this.shippingAddresses.find((address) => address.isDefault) ?? this.shippingAddresses[0];
          this.selectedAddressId = preferred?.id ?? '';
        }
        this.showForm = this.shippingAddresses.length === 0;
      },
      error: (error: unknown) => {
        this.errorMessage = readApiError(error, 'Addresses could not be loaded.');
      }
    });
  }
}
