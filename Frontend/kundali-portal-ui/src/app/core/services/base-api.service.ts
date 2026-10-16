// Base API Service - provides common HTTP methods and error handling
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, timeout } from 'rxjs/operators';
import { environment } from '../../config/environment/environment';

export interface ApiResponse<T> {
  success: boolean;
  data: T;
  message?: string;
  errors?: Record<string, string[]>;
}

@Injectable({
  providedIn: 'root',
})
export class BaseApiService {
  protected apiUrl = environment.apiBaseUrl;
  protected apiTimeout = environment.apiTimeout;

  constructor(protected http: HttpClient) {}

  /**
   * Generic GET request
   */
  protected get<T>(endpoint: string, options?: any): Observable<any> {
    return this.http
      .get<ApiResponse<T>>(`${this.apiUrl}/${endpoint}`, options)
      .pipe(
        timeout(this.apiTimeout),
        catchError((error) => this.handleError(error))
      );
  }

  /**
   * Generic POST request
   */
  protected post<T>(endpoint: string, body: any, options?: any): Observable<any> {
    return this.http
      .post<ApiResponse<T>>(`${this.apiUrl}/${endpoint}`, body, options)
      .pipe(
        timeout(this.apiTimeout),
        catchError((error) => this.handleError(error))
      );
  }

  /**
   * Generic PUT request
   */
  protected put<T>(endpoint: string, body: any, options?: any): Observable<any> {
    return this.http
      .put<ApiResponse<T>>(`${this.apiUrl}/${endpoint}`, body, options)
      .pipe(
        timeout(this.apiTimeout),
        catchError((error) => this.handleError(error))
      );
  }

  /**
   * Generic PATCH request
   */
  protected patch<T>(endpoint: string, body: any, options?: any): Observable<any> {
    return this.http
      .patch<ApiResponse<T>>(`${this.apiUrl}/${endpoint}`, body, options)
      .pipe(
        timeout(this.apiTimeout),
        catchError((error) => this.handleError(error))
      );
  }

  /**
   * Generic DELETE request
   */
  protected delete<T>(endpoint: string, options?: any): Observable<any> {
    return this.http
      .delete<ApiResponse<T>>(`${this.apiUrl}/${endpoint}`, options)
      .pipe(
        timeout(this.apiTimeout),
        catchError((error) => this.handleError(error))
      );
  }

  /**
   * Central error handling
   */
  protected handleError(error: any) {
    let errorMessage = 'An unexpected error occurred';

    if (error.error instanceof ErrorEvent) {
      // Client-side error
      errorMessage = error.error.message;
    } else if (error.status) {
      // Backend error
      switch (error.status) {
        case 400:
          errorMessage = error.error?.message || 'Bad Request';
          break;
        case 401:
          errorMessage = 'Unauthorized - Please login';
          break;
        case 403:
          errorMessage = 'Forbidden - Access denied';
          break;
        case 402:
          errorMessage = error.error?.message || 'This feature is not included in the current plan.';
          break;
        case 404:
          errorMessage = 'Resource not found';
          break;
        case 500:
          errorMessage = 'Internal Server Error';
          break;
        case 0:
          errorMessage = 'Network error - Unable to connect to server';
          break;
        default:
          errorMessage = `Error: ${error.status} ${error.statusText}`;
      }
    }

    console.error('API Error:', errorMessage, error);
    return throwError(() => new Error(errorMessage));
  }
}
