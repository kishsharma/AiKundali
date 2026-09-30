import { Component, OnDestroy, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, ParamMap, Router, RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import { readApiError } from '../../core/api/api-error';
import { AuthService } from '../../core/auth/auth.service';
import { formatMoney } from '../../core/format';
import { ProductDetail, ProductVariant, productTypeLabel } from '../../core/models/store.models';
import { CartService } from '../../core/services/cart.service';
import { CartStateService } from '../../core/services/cart-state.service';
import { CatalogService } from '../../core/services/catalog.service';

@Component({
  selector: 'app-product',
  imports: [FormsModule, RouterLink],
  template: `
    <main class="page" [class.is-refreshing]="loading && !!product">
      <p><a routerLink="/">Back to catalogue</a></p>
      @if (kundaliMatchScore !== null) {
        <section class="match-card">
          <h1>Matching with your kundali</h1>
          <div class="match-score" [attr.aria-label]="kundaliMatchScore + ' out of 100'">
            <strong>{{ kundaliMatchScore }}</strong>
            <span>/100</span>
          </div>
          @if (kundaliMatchArea) {
            <p class="match-area">{{ kundaliMatchArea }}</p>
          }
          <p>{{ kundaliMatchArea ? 'Suggested for this part of your Kundali, using the score stored in the report.' : 'This is the remedy suitability stored for this gemstone in your Kundali report.' }}</p>
        </section>
      }
      @if (errorMessage && !product) { <div class="error">{{ errorMessage }}</div> }
      @if (loading && !product) { <p class="muted">Loading product...</p> }

      @if (product) {
        <article class="detail">
          <div class="detail-visual">
            @if (activeImage && !imageFailed) {
              <img [src]="activeImage" [alt]="product.name" (error)="imageFailed = true">
            } @else {
              <span class="placeholder">{{ typeLabel(product.productType).charAt(0) }}</span>
            }
          </div>
          <div>
            <span class="badge">{{ typeLabel(product.productType) }}</span>
            <span class="badge">{{ product.categoryName }}</span>
            @if (kundaliMatchScore !== null) {
              <h2>{{ product.name }}</h2>
            } @else {
              <h1>{{ product.name }}</h1>
            }
            <p>{{ product.shortDescription }}</p>
            <p class="muted">{{ product.description }}</p>
            <p>
              <span class="price">{{ money(selectedPrice, product.currency) }}</span>
              @if (product.compareAtPrice) {
                <span class="compare">{{ money(product.compareAtPrice, product.currency) }}</span>
              }
            </p>
            <p class="muted">{{ stockText }}</p>
            @if (product.requiresConsultation) {
              <div class="warning">This remedy is listed for review. Fulfilment may need a suitability consultation.</div>
            }

            @if (activeVariants.length) {
              <label class="field">
                <span>Variant</span>
                <select [(ngModel)]="selectedVariantId">
                  @for (variant of activeVariants; track variant.id) {
                    <option [ngValue]="variant.id">{{ variant.name }} · {{ money(variant.price, product.currency) }} · {{ variant.availableQuantity }} in stock</option>
                  }
                </select>
              </label>
            }

            <label class="field">
              <span>Quantity</span>
              <input type="number" min="1" max="99" [(ngModel)]="quantity">
            </label>

            @if (errorMessage) { <div class="error">{{ errorMessage }}</div> }
            @if (added) { <div class="success">Added to cart.</div> }

            <button class="btn" type="button" [disabled]="saving || !canAdd" (click)="addToCart()">
              {{ saving ? 'Adding...' : 'Add to cart' }}
            </button>
          </div>
        </article>
      }
    </main>
  `
})
export class ProductPage implements OnInit, OnDestroy {
  product: ProductDetail | null = null;
  selectedVariantId: string | null = null;
  quantity = 1;
  loading = false;
  saving = false;
  added = false;
  imageFailed = false;
  errorMessage = '';
  kundaliMatchScore: number | null = null;
  kundaliMatchArea = '';
  private routeSub?: Subscription;
  private productSub?: Subscription;

  constructor(
    private readonly catalog: CatalogService,
    private readonly carts: CartService,
    private readonly cartState: CartStateService,
    private readonly auth: AuthService,
    private readonly route: ActivatedRoute,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    this.routeSub = this.route.paramMap.subscribe((params) => {
      this.kundaliMatchScore = readKundaliMatchScore(this.route.snapshot.queryParamMap);
      this.kundaliMatchArea = this.kundaliMatchScore === null
        ? ''
        : (this.route.snapshot.queryParamMap.get('area') ?? '').trim().slice(0, 40);
      this.loadProduct(params.get('slug') ?? '');
    });
  }

  ngOnDestroy(): void {
    this.routeSub?.unsubscribe();
    this.productSub?.unsubscribe();
  }

  private loadProduct(slug: string): void {
    this.productSub?.unsubscribe();
    this.loading = true;
    this.errorMessage = '';
    this.added = false;
    this.imageFailed = false;
    this.productSub = this.catalog.getProduct(slug).subscribe({
      next: (product) => {
        this.loading = false;
        this.product = product;
        this.selectedVariantId = this.activeVariants[0]?.id ?? null;
        this.quantity = 1;
      },
      error: (error: unknown) => {
        this.loading = false;
        this.product = null;
        this.errorMessage = readApiError(error, 'This product could not be found.');
      }
    });
  }

  get activeVariants(): ProductVariant[] {
    return (this.product?.variants ?? []).filter((variant) => variant.isActive);
  }

  get selectedPrice(): number {
    const variant = this.activeVariants.find((item) => item.id === this.selectedVariantId);
    return variant?.price ?? this.product?.basePrice ?? 0;
  }

  get activeImage(): string | null {
    const images = this.product?.images ?? [];
    const variantImage = images.find((image) => image.variantId === this.selectedVariantId);
    return variantImage?.imageUrl ?? images.find((image) => image.isPrimary)?.imageUrl ?? images[0]?.imageUrl ?? null;
  }

  get stockText(): string {
    const variant = this.activeVariants.find((item) => item.id === this.selectedVariantId);
    const available = variant?.availableQuantity ?? this.product?.availableQuantity ?? 0;
    return available > 0 ? `${available} available` : 'Out of stock';
  }

  get canAdd(): boolean {
    if (!this.product?.inStock || this.quantity < 1) {
      return false;
    }
    if (this.activeVariants.length > 0 && !this.selectedVariantId) {
      return false;
    }
    const variant = this.activeVariants.find((item) => item.id === this.selectedVariantId);
    const available = variant?.availableQuantity ?? this.product.availableQuantity;
    return this.quantity <= available;
  }

  money(amount: number, currency: string): string {
    return formatMoney(amount, currency);
  }

  typeLabel(value: string): string {
    return productTypeLabel(value);
  }

  addToCart(): void {
    if (!this.product) {
      return;
    }

    if (!this.auth.isLoggedIn()) {
      void this.router.navigate(['/login'], {
        queryParams: { returnUrl: this.router.url }
      });
      return;
    }

    this.saving = true;
    this.added = false;
    this.errorMessage = '';
    this.carts.addItem(this.product.id, this.quantity, this.selectedVariantId).subscribe({
      next: (cart) => {
        this.saving = false;
        this.added = true;
        this.cartState.replace(cart);
      },
      error: (error: unknown) => {
        this.saving = false;
        this.errorMessage = readApiError(error, 'Could not add this item to the cart.');
      }
    });
  }
}

function readKundaliMatchScore(params: ParamMap): number | null {
  if (params.get('kundaliMatch') !== '1') {
    return null;
  }

  const score = Number(params.get('score'));
  if (!Number.isFinite(score)) {
    return null;
  }

  return Math.max(0, Math.min(100, Math.round(score)));
}
