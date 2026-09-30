import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../config/environment';
import { Category, PagedResult, ProductDetail, ProductListItem, ProductQuery } from '../models/store.models';

@Injectable({ providedIn: 'root' })
export class CatalogService {
  private readonly baseUrl = `${environment.apiBaseUrl}/store`;

  constructor(private readonly http: HttpClient) {}

  getCategories(): Observable<Category[]> {
    return this.http.get<Category[]>(`${this.baseUrl}/categories`);
  }

  getProducts(query: ProductQuery): Observable<PagedResult<ProductListItem>> {
    let params = new HttpParams()
      .set('pageNumber', query.pageNumber)
      .set('pageSize', query.pageSize)
      .set('sortBy', query.sortBy)
      .set('sortDirection', query.sortDirection);

    if (query.category) {
      params = params.set('category', query.category);
    }
    if (query.search) {
      params = params.set('search', query.search);
    }
    if (query.productType) {
      params = params.set('productType', query.productType);
    }
    if (query.isFeatured) {
      params = params.set('isFeatured', 'true');
    }

    return this.http.get<PagedResult<ProductListItem>>(`${this.baseUrl}/products`, { params });
  }

  getProduct(slug: string): Observable<ProductDetail> {
    return this.http.get<ProductDetail>(`${this.baseUrl}/products/${encodeURIComponent(slug)}`);
  }
}
