import { Injectable, signal } from '@angular/core';

export type ConfirmDialogTone = 'default' | 'danger';

export interface ConfirmDialogOptions {
  title: string;
  message: string;
  confirmLabel?: string;
  cancelLabel?: string;
  tone?: ConfirmDialogTone;
}

export interface ConfirmDialogRequest extends Required<ConfirmDialogOptions> {
  resolve: (confirmed: boolean) => void;
}

@Injectable({
  providedIn: 'root',
})
export class ConfirmDialogService {
  private readonly requestState = signal<ConfirmDialogRequest | null>(null);
  readonly current = this.requestState.asReadonly();

  confirm(options: ConfirmDialogOptions): Promise<boolean> {
    this.finish(false);

    return new Promise((resolve) => {
      this.requestState.set({
        title: options.title,
        message: options.message,
        confirmLabel: options.confirmLabel || 'Confirm',
        cancelLabel: options.cancelLabel || 'Cancel',
        tone: options.tone || 'default',
        resolve,
      });
    });
  }

  choose(confirmed: boolean): void {
    this.finish(confirmed);
  }

  private finish(confirmed: boolean): void {
    const request = this.requestState();
    if (!request) {
      return;
    }

    this.requestState.set(null);
    request.resolve(confirmed);
  }
}
