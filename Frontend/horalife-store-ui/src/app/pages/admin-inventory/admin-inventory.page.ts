import { Component, OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { readApiError } from '../../core/api/api-error';
import {
  InventoryListItem,
  InventoryTransactionListItem
} from '../../core/models/inventory.models';
import { InventoryAdminService } from '../../core/services/inventory-admin.service';

@Component({
  selector: 'app-admin-inventory',
  imports: [FormsModule, DatePipe],
  template: `
    <main class="page catalog-page">
      <div class="page-head">
        <div>
          <h1>Inventory</h1>
          <p class="muted">Stock, reservations, reorder levels and ledger for store SKUs.</p>
        </div>
        <label class="chip">
          <input type="checkbox" [ngModel]="lowStockOnly" (ngModelChange)="setLowStockOnly($event)">
          Low stock only
        </label>
      </div>

      @if (errorMessage) { <div class="error">{{ errorMessage }}</div> }
      @if (successMessage) { <div class="success">{{ successMessage }}</div> }
      @if (loading) { <p class="muted">Loading inventory...</p> }

      @if (!loading && items.length === 0) {
        <div class="panel">
          <p>No inventory rows match this filter.</p>
        </div>
      }

      @if (items.length) {
        <div class="panel inventory-table-wrap">
          <table class="inventory-table">
            <thead>
              <tr>
                <th>Product</th>
                <th>SKU</th>
                <th>Stock</th>
                <th>Reserved</th>
                <th>Available</th>
                <th>Reorder</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              @for (item of items; track item.id) {
                <tr [class.low-stock]="item.isLowStock">
                  <td>
                    <strong>{{ item.productName }}</strong>
                    @if (item.variantName) {
                      <div class="muted">{{ item.variantName }}</div>
                    }
                  </td>
                  <td>{{ item.variantSku || item.productSku }}</td>
                  <td>{{ item.stockQuantity }}</td>
                  <td>{{ item.reservedQuantity }}</td>
                  <td>{{ item.availableQuantity }}</td>
                  <td>{{ item.reorderLevel }}</td>
                  <td class="inventory-actions">
                    <button class="btn-secondary" type="button" (click)="openAdjust(item)">Adjust</button>
                    <button class="btn-secondary" type="button" (click)="openReorder(item)">Reorder</button>
                    <button class="btn-secondary" type="button" (click)="openHistory(item)">History</button>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }

      @if (activeItem && drawerMode === 'adjust') {
        <div class="inventory-drawer panel">
          <h2>Adjust stock</h2>
          <p class="muted">{{ activeItem.productName }}{{ activeItem.variantName ? ' · ' + activeItem.variantName : '' }}</p>
          <label class="field">
            <span>Quantity change (+ inbound / − reduce)</span>
            <input type="number" [(ngModel)]="adjustDelta">
          </label>
          <label class="field">
            <span>Type</span>
            <select [(ngModel)]="adjustType">
              <option value="stock_added">stock_added</option>
              <option value="manual_adjustment">manual_adjustment</option>
            </select>
          </label>
          <label class="field">
            <span>Reason</span>
            <input [(ngModel)]="adjustReason" placeholder="Optional note">
          </label>
          <div class="row-between">
            <button class="btn-secondary" type="button" (click)="closeDrawer()">Cancel</button>
            <button class="btn" type="button" [disabled]="busy" (click)="submitAdjust()">Save</button>
          </div>
        </div>
      }

      @if (activeItem && drawerMode === 'reorder') {
        <div class="inventory-drawer panel">
          <h2>Reorder level</h2>
          <p class="muted">{{ activeItem.productName }}{{ activeItem.variantName ? ' · ' + activeItem.variantName : '' }}</p>
          <label class="field">
            <span>Reorder level</span>
            <input type="number" min="0" [(ngModel)]="reorderLevel">
          </label>
          <div class="row-between">
            <button class="btn-secondary" type="button" (click)="closeDrawer()">Cancel</button>
            <button class="btn" type="button" [disabled]="busy" (click)="submitReorder()">Save</button>
          </div>
        </div>
      }

      @if (activeItem && drawerMode === 'history') {
        <div class="inventory-drawer panel">
          <h2>Ledger</h2>
          <p class="muted">{{ activeItem.productName }}{{ activeItem.variantName ? ' · ' + activeItem.variantName : '' }}</p>
          @if (historyLoading) { <p class="muted">Loading history...</p> }
          @if (!historyLoading && transactions.length === 0) {
            <p class="muted">No transactions yet.</p>
          }
          <ul class="inventory-history">
            @for (txn of transactions; track txn.id) {
              <li>
                <strong>{{ txn.transactionType }}</strong>
                <span>{{ txn.quantityChange >= 0 ? '+' : '' }}{{ txn.quantityChange }}</span>
                <span class="muted">{{ txn.createdAt | date:'medium' }}</span>
                @if (txn.notes) { <div class="muted">{{ txn.notes }}</div> }
              </li>
            }
          </ul>
          <button class="btn-secondary" type="button" (click)="closeDrawer()">Close</button>
        </div>
      }
    </main>
  `
})
export class AdminInventoryPage implements OnInit {
  items: InventoryListItem[] = [];
  transactions: InventoryTransactionListItem[] = [];
  loading = false;
  historyLoading = false;
  busy = false;
  lowStockOnly = false;
  errorMessage = '';
  successMessage = '';
  activeItem: InventoryListItem | null = null;
  drawerMode: 'adjust' | 'reorder' | 'history' | null = null;
  adjustDelta = 1;
  adjustType: 'stock_added' | 'manual_adjustment' = 'stock_added';
  adjustReason = '';
  reorderLevel = 0;

  constructor(private readonly inventory: InventoryAdminService) {}

  ngOnInit(): void {
    this.reload();
  }

  setLowStockOnly(value: boolean): void {
    this.lowStockOnly = value;
    this.reload();
  }

  openAdjust(item: InventoryListItem): void {
    this.activeItem = item;
    this.drawerMode = 'adjust';
    this.adjustDelta = 1;
    this.adjustType = 'stock_added';
    this.adjustReason = '';
    this.clearMessages();
  }

  openReorder(item: InventoryListItem): void {
    this.activeItem = item;
    this.drawerMode = 'reorder';
    this.reorderLevel = item.reorderLevel;
    this.clearMessages();
  }

  openHistory(item: InventoryListItem): void {
    this.activeItem = item;
    this.drawerMode = 'history';
    this.transactions = [];
    this.historyLoading = true;
    this.clearMessages();
    this.inventory.transactions(item.id).subscribe({
      next: (rows) => {
        this.historyLoading = false;
        this.transactions = rows;
      },
      error: (error: unknown) => {
        this.historyLoading = false;
        this.errorMessage = readApiError(error, 'Could not load inventory history.');
      }
    });
  }

  closeDrawer(): void {
    this.activeItem = null;
    this.drawerMode = null;
  }

  submitAdjust(): void {
    if (!this.activeItem) {
      return;
    }

    this.busy = true;
    this.clearMessages();
    this.inventory.adjust(this.activeItem.id, {
      quantityChange: this.adjustDelta,
      reason: this.adjustReason.trim() || undefined,
      transactionType: this.adjustType
    }).subscribe({
      next: () => {
        this.busy = false;
        this.successMessage = 'Stock updated.';
        this.closeDrawer();
        this.reload();
      },
      error: (error: unknown) => {
        this.busy = false;
        this.errorMessage = readApiError(error, 'Stock adjustment failed.');
      }
    });
  }

  submitReorder(): void {
    if (!this.activeItem) {
      return;
    }

    this.busy = true;
    this.clearMessages();
    this.inventory.setReorderLevel(this.activeItem.id, {
      reorderLevel: this.reorderLevel
    }).subscribe({
      next: () => {
        this.busy = false;
        this.successMessage = 'Reorder level updated.';
        this.closeDrawer();
        this.reload();
      },
      error: (error: unknown) => {
        this.busy = false;
        this.errorMessage = readApiError(error, 'Reorder level update failed.');
      }
    });
  }

  private reload(): void {
    this.loading = true;
    this.errorMessage = '';
    this.inventory.list(this.lowStockOnly).subscribe({
      next: (items) => {
        this.loading = false;
        this.items = items;
      },
      error: (error: unknown) => {
        this.loading = false;
        this.items = [];
        this.errorMessage = readApiError(error, 'Inventory could not be loaded.');
      }
    });
  }

  private clearMessages(): void {
    this.errorMessage = '';
    this.successMessage = '';
  }
}
