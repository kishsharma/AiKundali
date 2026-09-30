import { Component, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { readApiError } from '../../core/api/api-error';
import { formatMoney } from '../../core/format';
import { Cart } from '../../core/models/store.models';
import { CartService } from '../../core/services/cart.service';
import { CartStateService } from '../../core/services/cart-state.service';

@Component({
  selector: 'app-cart',
  imports: [RouterLink],
  template: `
    <main class="page">
      <div class="page-head">
        <div>
          <h1>Cart</h1>
          <p class="muted">Prices are confirmed again at checkout.</p>
        </div>
        @if (cart?.items?.length) {
          <button class="btn-secondary" type="button" (click)="clear()" [disabled]="busy">Clear cart</button>
        }
      </div>

      @if (errorMessage) { <div class="error">{{ errorMessage }}</div> }
      @if (loading) { <p class="muted">Loading cart...</p> }

      @if (!loading && !cart?.items?.length) {
        <div class="panel">
          <p>Your cart is empty.</p>
          <a class="btn" routerLink="/">Browse the catalogue</a>
        </div>
      }

      <section class="stack">
        @for (item of cart?.items ?? []; track item.id) {
          <article class="line row-between">
            <div>
              <a [routerLink]="['/products', item.productSlug]"><strong>{{ item.productName }}</strong></a>
              @if (item.variantName) { <div class="muted">{{ item.variantName }}</div> }
              <div class="muted">{{ money(item.unitPrice, item.currency) }} each · {{ item.availableQuantity }} available</div>
              @if (item.requiresConsultation) { <div class="badge">Consultation</div> }
            </div>
            <div class="actions">
              <button class="btn-secondary" type="button" (click)="change(item.id, item.quantity - 1)" [disabled]="busy || item.quantity <= 1">−</button>
              <span>{{ item.quantity }}</span>
              <button class="btn-secondary" type="button" (click)="change(item.id, item.quantity + 1)" [disabled]="busy || item.quantity >= 99 || item.quantity >= item.availableQuantity">+</button>
              <strong class="price">{{ money(item.lineTotal, item.currency) }}</strong>
              <button class="btn-secondary" type="button" (click)="remove(item.id)" [disabled]="busy">Remove</button>
            </div>
          </article>
        }
      </section>

      @if (cart?.items?.length) {
        <div class="panel" style="margin-top: 16px;">
          <div class="row-between">
            <span>{{ cart?.totalQuantity }} items</span>
            <strong class="price">{{ money(cart?.subtotal ?? 0, cart?.currency ?? 'INR') }}</strong>
          </div>
          <p><a class="btn" routerLink="/checkout">Checkout</a></p>
        </div>
      }
    </main>
  `
})
export class CartPage implements OnInit {
  cart: Cart | null = null;
  loading = false;
  busy = false;
  errorMessage = '';

  constructor(
    private readonly carts: CartService,
    private readonly cartState: CartStateService
  ) {}

  ngOnInit(): void {
    this.loading = true;
    this.carts.getCart().subscribe({
      next: (cart) => {
        this.loading = false;
        this.apply(cart);
      },
      error: (error: unknown) => {
        this.loading = false;
        this.errorMessage = readApiError(error, 'The cart could not be loaded.');
      }
    });
  }

  money(amount: number, currency: string): string {
    return formatMoney(amount, currency);
  }

  change(cartItemId: string, quantity: number): void {
    this.busy = true;
    this.errorMessage = '';
    this.carts.updateQuantity(cartItemId, quantity).subscribe({
      next: (cart) => {
        this.busy = false;
        this.apply(cart);
      },
      error: (error: unknown) => {
        this.busy = false;
        this.errorMessage = readApiError(error, 'Quantity could not be updated.');
      }
    });
  }

  remove(cartItemId: string): void {
    this.busy = true;
    this.errorMessage = '';
    this.carts.removeItem(cartItemId).subscribe({
      next: () => this.reload(),
      error: (error: unknown) => {
        this.busy = false;
        this.errorMessage = readApiError(error, 'The item could not be removed.');
      }
    });
  }

  clear(): void {
    this.busy = true;
    this.carts.clear().subscribe({
      next: () => this.reload(),
      error: (error: unknown) => {
        this.busy = false;
        this.errorMessage = readApiError(error, 'The cart could not be cleared.');
      }
    });
  }

  private reload(): void {
    this.carts.getCart().subscribe({
      next: (cart) => {
        this.busy = false;
        this.apply(cart);
      },
      error: () => {
        this.busy = false;
      }
    });
  }

  private apply(cart: Cart): void {
    this.cart = cart;
    this.cartState.replace(cart);
  }
}
