import { Injectable, signal } from '@angular/core';

import { DhruvaKundaliContext } from './dhruva-kundali-context.mapper';

@Injectable({
  providedIn: 'root',
})
export class DhruvaKundaliContextService {
  readonly context = signal<DhruvaKundaliContext | null>(null);

  setContext(context: DhruvaKundaliContext): void {
    this.context.set(context);
  }

  clearContext(): void {
    this.context.set(null);
  }
}