import { Component, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { formatMoney, statusLabel } from '../../core/format';
import { PlacedOrder } from '../../core/models/store.models';

const LAST_ORDER_KEY = 'horalife_store_last_order';

@Component({
  selector: 'app-order-received',
  imports: [RouterLink],
  template: `
    <main class="page">
      @if (!placed) {
        <div class="panel">
          <h1>No recent order</h1>
          <p class="muted">Place an order from the cart to see the confirmation here.</p>
          <a class="btn" routerLink="/cart">Go to cart</a>
        </div>
      } @else {
        <div class="panel">
          <h1>Order received</h1>
          <p>Order number <strong>{{ placed.order.orderNumber }}</strong></p>
          <p>Status: {{ label(placed.converted.orderStatus || placed.order.orderStatus) }}</p>
          <p>Payment: {{ label(placed.converted.paymentStatus || placed.order.paymentStatus) }}</p>
          <p class="price">{{ money(placed.order.totalAmount, placed.order.currency) }}</p>
          <div class="warning">Payment capture is not part of this storefront yet. The order stays pending until payment is added.</div>
          <p class="muted">{{ placed.converted.orderItemsCount }} line(s). Inventory reserved: {{ placed.converted.inventoryReserved ? 'yes' : 'no' }}. Cart converted: {{ placed.converted.cartConverted ? 'yes' : 'no' }}.</p>
          <a class="btn" routerLink="/">Continue browsing</a>
        </div>
      }
    </main>
  `
})
export class OrderReceivedPage implements OnInit {
  placed: PlacedOrder | null = null;

  ngOnInit(): void {
    const raw = sessionStorage.getItem(LAST_ORDER_KEY);
    if (!raw) {
      return;
    }

    try {
      this.placed = JSON.parse(raw) as PlacedOrder;
    } catch {
      this.placed = null;
    }
  }

  money(amount: number, currency: string): string {
    return formatMoney(amount, currency);
  }

  label(value: string): string {
    return statusLabel(value);
  }
}
