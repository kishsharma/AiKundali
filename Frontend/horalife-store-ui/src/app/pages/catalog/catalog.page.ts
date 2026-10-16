import { Component, OnDestroy, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import { readApiError } from '../../core/api/api-error';
import { formatMoney } from '../../core/format';
import { Category, PRODUCT_TYPES, PagedResult, ProductDetail, ProductListItem, productTypeLabel } from '../../core/models/store.models';
import { CatalogService } from '../../core/services/catalog.service';

@Component({
  selector: 'app-catalog',
  imports: [FormsModule, RouterLink],
  template: `
    <main class="page catalog-page">
      <section class="catalog-hero" aria-labelledby="catalogHeroTitle">
        <div class="catalog-hero-copy">
          <p class="catalog-hero-kicker">HoraLife Store</p>
          <h1 id="catalogHeroTitle">Remedies and ritual items for your chart</h1>
          <p>Browse gemstones, yantras, rudraksha, and puja items chosen to support Vedic practice.</p>
        </div>
        <div class="catalog-hero-visual" aria-hidden="true">
          <img class="hero-shot hero-shot--main" src="assets/store/products/five-mukhi-rudraksha.webp" alt="">
          <img class="hero-shot hero-shot--mid" src="assets/store/products/shree-yantra.webp" alt="">
          <img class="hero-shot hero-shot--side" src="assets/store/products/yellow-sapphire-pukhraj.png" alt="">
        </div>
      </section>

      <ul class="trust-strip" aria-label="Store promises">
        <li>
          <span class="trust-mark" aria-hidden="true"></span>
          <div>
            <strong>Chart-aware suggestions</strong>
            <span>Gemstone matches can come from your Kundali reading.</span>
          </div>
        </li>
        <li>
          <span class="trust-mark" aria-hidden="true"></span>
          <div>
            <strong>Authentic store items</strong>
            <span>Rudraksha, yantras, and gemstones from the HoraLife catalogue.</span>
          </div>
        </li>
        <li>
          <span class="trust-mark" aria-hidden="true"></span>
          <div>
            <strong>Secure cart checkout</strong>
            <span>Sign in to save your cart and complete an order.</span>
          </div>
        </li>
      </ul>

      <div class="catalog-layout">
        <section class="filters filters-side" aria-label="Catalogue filters">
          <div class="filter-block">
            <h2 class="filter-heading">Categories</h2>
            <div class="chip-row">
              <button type="button" class="chip" [class.active]="!category" (click)="setCategory('')">All</button>
              @for (item of categories; track item.id) {
                <button type="button" class="chip" [class.active]="category === item.slug" (click)="setCategory(item.slug)">
                  {{ item.name }} ({{ item.productCount }})
                </button>
              }
            </div>
          </div>
          <div class="filter-block">
            <h2 class="filter-heading">Type</h2>
            <label class="field">
              <span class="sr-only">Product type</span>
              <select [ngModel]="productType" (ngModelChange)="setType($event)">
                <option value="">Any</option>
                @for (type of productTypes; track type.value) {
                  <option [value]="type.value">{{ type.label }}</option>
                }
              </select>
            </label>
          </div>
          <div class="filter-block">
            <h2 class="filter-heading">Sort</h2>
            <label class="field">
              <span class="sr-only">Sort products</span>
              <select [ngModel]="sortBy" (ngModelChange)="setSort($event)">
                <option value="display_order">Featured order</option>
                <option value="name">Name</option>
                <option value="price">Price</option>
                <option value="newest">Newest</option>
              </select>
            </label>
            <label class="chip featured-chip">
              <input type="checkbox" [ngModel]="featuredOnly" (ngModelChange)="setFeatured($event)"> Featured only
            </label>
          </div>
        </section>

        <div class="catalog-main">
          <label class="catalog-search field">
            <span>Search</span>
            <input [ngModel]="search" (ngModelChange)="onSearch($event)" placeholder="Search rudraksha, gemstones, yantra...">
          </label>

          @if (kundaliMatchScore !== null && matchedProduct) {
            <a class="match-card match-stone" [routerLink]="['/products', matchedProduct.slug]" [queryParams]="matchQuery">
              <div class="thumb">
                @if (matchedImage && !failedImages.has(matchedProduct.id)) {
                  <img [src]="matchedImage" [alt]="matchedProduct.name" (error)="markImageFailed(matchedProduct.id)">
                } @else {
                  <span class="placeholder">{{ typeLabel(matchedProduct.productType).charAt(0) }}</span>
                }
              </div>
              <div>
                <p class="match-kicker">From your chart</p>
                <h2>{{ matchedProduct.name }}</h2>
                @if (kundaliMatchArea) {
                  <p class="match-area">{{ kundaliMatchArea }}</p>
                }
                <p>{{ kundaliMatchArea ? 'Suggested for this part of your Kundali, using the score stored in the report.' : 'This is the remedy suitability stored for this gemstone in your Kundali report.' }}</p>
              </div>
              <div class="match-score" [attr.aria-label]="kundaliMatchScore + ' out of 100'">
                <strong>{{ kundaliMatchScore }}</strong>
                <span>/100</span>
              </div>
            </a>
          }

          @if (errorMessage) { <div class="error">{{ errorMessage }}</div> }
          @if (loading && !result) { <p class="muted">Loading catalogue...</p> }

          @if (!loading && result && result.items.length === 0) {
            <div class="panel">
              <p>No products match these filters.</p>
              <p class="muted">If the catalogue is empty, load <code>horalife_store_master_seed.sql</code> into the local horalife database.</p>
            </div>
          }

          <section class="grid" [class.is-refreshing]="loading && !!result" [attr.aria-busy]="loading">
            @for (product of result?.items ?? []; track product.id) {
              <a class="product-card" [routerLink]="['/products', product.slug]">
                <div class="thumb">
                  @if (product.primaryImageUrl && !failedImages.has(product.id)) {
                    <img [src]="product.primaryImageUrl" [alt]="product.primaryImageAltText || product.name" (error)="markImageFailed(product.id)">
                  } @else {
                    <span class="placeholder">{{ typeLabel(product.productType).charAt(0) }}</span>
                  }
                  <div class="card-badges">
                    <span class="badge">{{ typeLabel(product.productType) }}</span>
                    @if (product.isFeatured) { <span class="badge badge-accent">Featured</span> }
                    @if (product.requiresConsultation) { <span class="badge">Consultation</span> }
                  </div>
                </div>
                <div class="card-body">
                  <strong class="card-title">{{ product.name }}</strong>
                  @if (product.shortDescription) {
                    <p class="card-copy muted">{{ product.shortDescription }}</p>
                  }
                  <div class="card-price-row">
                    <span class="price">{{ money(product.displayPrice, product.currency) }}</span>
                    @if (product.compareAtPrice) {
                      <span class="compare">{{ money(product.compareAtPrice, product.currency) }}</span>
                    }
                  </div>
                  <span class="card-stock muted">{{ product.inStock ? product.availableQuantity + ' available' : 'Out of stock' }}</span>
                </div>
              </a>
            }
          </section>

          @if (result && result.totalPages > 1) {
            <div class="pager">
              <button class="btn-secondary" type="button" [disabled]="!result.hasPreviousPage" (click)="setPage(result.pageNumber - 1)">Previous</button>
              <span class="muted">Page {{ result.pageNumber }} of {{ result.totalPages }}</span>
              <button class="btn-secondary" type="button" [disabled]="!result.hasNextPage" (click)="setPage(result.pageNumber + 1)">Next</button>
            </div>
          }
        </div>
      </div>
    </main>
  `
})
export class CatalogPage implements OnInit, OnDestroy {
  readonly productTypes = PRODUCT_TYPES;
  categories: Category[] = [];
  result: PagedResult<ProductListItem> | null = null;
  category = '';
  search = '';
  productType = '';
  featuredOnly = false;
  sortBy = 'display_order';
  pageNumber = 1;
  loading = false;
  errorMessage = '';
  kundaliMatchScore: number | null = null;
  kundaliMatchArea = '';
  matchedProduct: ProductDetail | null = null;
  readonly failedImages = new Set<string>();
  private matchedSlug = '';
  private searchTimer?: ReturnType<typeof setTimeout>;
  private routeSub?: Subscription;
  private matchSub?: Subscription;

  constructor(
    private readonly catalog: CatalogService,
    private readonly route: ActivatedRoute,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    this.catalog.getCategories().subscribe({
      next: (categories) => this.categories = categories,
      error: () => this.categories = []
    });

    this.routeSub = this.route.queryParamMap.subscribe((params) => {
      this.category = params.get('category') ?? '';
      this.search = params.get('search') ?? '';
      this.productType = params.get('productType') ?? '';
      this.featuredOnly = params.get('featured') === '1';
      this.sortBy = params.get('sort') ?? 'display_order';
      this.pageNumber = Number(params.get('page') ?? '1') || 1;
      this.kundaliMatchScore = readKundaliMatchScore(params);
      this.kundaliMatchArea = this.kundaliMatchScore === null
        ? ''
        : (params.get('area') ?? '').trim().slice(0, 40);
      const slug = this.kundaliMatchScore === null ? '' : (params.get('gemstone') ?? '').trim();
      if (slug !== this.matchedSlug) {
        this.matchedSlug = slug;
        this.loadMatch(slug);
      }
      this.fetch();
    });
  }

  ngOnDestroy(): void {
    this.routeSub?.unsubscribe();
    this.matchSub?.unsubscribe();
    if (this.searchTimer) {
      clearTimeout(this.searchTimer);
    }
  }

  money(amount: number, currency: string): string {
    return formatMoney(amount, currency);
  }

  typeLabel(value: string): string {
    return productTypeLabel(value);
  }

  markImageFailed(id: string): void {
    this.failedImages.add(id);
  }

  get matchQuery(): Record<string, string> {
    const query: Record<string, string> = {
      kundaliMatch: '1',
      score: String(this.kundaliMatchScore ?? 0)
    };
    if (this.kundaliMatchArea) {
      query['area'] = this.kundaliMatchArea;
    }
    if (this.matchedSlug) {
      query['gemstone'] = this.matchedSlug;
    }
    return query;
  }

  get matchedImage(): string | null {
    const images = this.matchedProduct?.images ?? [];
    return images.find((image) => image.isPrimary)?.imageUrl ?? images[0]?.imageUrl ?? null;
  }

  setCategory(slug: string): void {
    this.updateQuery({ category: slug || null, page: null });
  }

  setType(value: string): void {
    this.updateQuery({ productType: value || null, page: null });
  }

  setSort(value: string): void {
    this.updateQuery({ sort: value });
  }

  setFeatured(value: boolean): void {
    this.updateQuery({ featured: value ? '1' : null, page: null });
  }

  setPage(page: number): void {
    this.updateQuery({ page: String(page) });
  }

  onSearch(value: string): void {
    this.search = value;
    if (this.searchTimer) {
      clearTimeout(this.searchTimer);
    }
    this.searchTimer = setTimeout(() => {
      this.updateQuery({ search: value.trim() || null, page: null });
    }, 300);
  }

  private loadMatch(slug: string): void {
    this.matchSub?.unsubscribe();
    this.matchedProduct = null;
    if (!slug) {
      return;
    }

    this.matchSub = this.catalog.getProduct(slug).subscribe({
      next: (product) => {
        this.matchedProduct = product;
      },
      error: () => {
        this.matchedProduct = null;
      }
    });
  }

  private fetch(): void {
    this.loading = true;
    this.errorMessage = '';
    this.catalog.getProducts({
      category: this.category || undefined,
      search: this.search || undefined,
      productType: this.productType || undefined,
      isFeatured: this.featuredOnly || undefined,
      pageNumber: this.pageNumber,
      pageSize: 12,
      sortBy: this.sortBy,
      sortDirection: 'asc'
    }).subscribe({
      next: (result) => {
        this.loading = false;
        this.result = result;
      },
      error: (error: unknown) => {
        this.loading = false;
        this.result = null;
        this.errorMessage = readApiError(error, 'The catalogue could not be loaded.');
      }
    });
  }

  private updateQuery(changes: Record<string, string | null>): void {
    const queryParams = { ...this.route.snapshot.queryParams, ...changes };
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams,
      queryParamsHandling: 'merge'
    });
  }
}

function readKundaliMatchScore(params: { get(name: string): string | null }): number | null {
  if (params.get('kundaliMatch') !== '1') {
    return null;
  }

  const score = Number(params.get('score'));
  if (!Number.isFinite(score)) {
    return null;
  }

  return Math.max(0, Math.min(100, Math.round(score)));
}
