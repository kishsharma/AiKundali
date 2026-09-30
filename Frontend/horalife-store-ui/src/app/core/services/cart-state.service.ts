import { Injectable, computed, signal } from '@angular/core';
import { Cart } from '../models/store.models';
import { AuthService } from '../auth/auth.service';
import { CartService } from './cart.service';

@Injectable({ providedIn: 'root' })
export class CartStateService {
  readonly cart = signal<Cart | null>(null);
  readonly count = computed(() => this.cart()?.totalQuantity ?? 0);

  constructor(
    private readonly auth: AuthService,
    private readonly carts: CartService
  ) {}

  refresh(): void {
    if (!this.auth.isLoggedIn()) {
      this.cart.set(null);
      return;
    }

    this.carts.getCart().subscribe({
      next: (cart) => this.cart.set(cart),
      error: () => this.cart.set(null)
    });
  }

  replace(cart: Cart): void {
    this.cart.set(cart);
  }

  clearLocal(): void {
    this.cart.set(null);
  }
}
