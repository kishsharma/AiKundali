import { Injectable, signal } from '@angular/core';

import { DhruvaKundaliContext } from './dhruva-kundali-context.mapper';

@Injectable({
  providedIn: 'root',
})
export class DhruvaKundaliContextService {
  readonly context = signal<DhruvaKundaliContext | null>(null);
  readonly source = signal<unknown | null>(null);

  setContext(context: DhruvaKundaliContext, source?: unknown): void {
    this.context.set(context);
    this.source.set(source ?? null);
  }

  clearContext(): void {
    this.context.set(null);
    this.source.set(null);
  }
}
