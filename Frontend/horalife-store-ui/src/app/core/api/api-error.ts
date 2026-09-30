import { HttpErrorResponse } from '@angular/common/http';

export function readApiError(error: unknown, fallback: string): string {
  if (!(error instanceof HttpErrorResponse)) {
    return fallback;
  }

  if (error.status === 0) {
    return 'Cannot reach the HoraLife API. Start KundaliEngine.API and try again.';
  }

  const body = error.error as { title?: string; detail?: string; message?: string } | string | null;
  if (typeof body === 'string' && body.trim()) {
    return body;
  }

  if (body && typeof body === 'object') {
    return body.detail || body.message || body.title || fallback;
  }

  return fallback;
}
