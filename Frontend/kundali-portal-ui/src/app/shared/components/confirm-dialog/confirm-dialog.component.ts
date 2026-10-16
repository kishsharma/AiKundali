import { CommonModule } from '@angular/common';
import { Component, ElementRef, HostListener, ViewChild, effect, inject } from '@angular/core';
import { ConfirmDialogService } from './confirm-dialog.service';

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './confirm-dialog.component.html',
  styleUrl: './confirm-dialog.component.scss',
})
export class ConfirmDialogComponent {
  readonly confirmDialog = inject(ConfirmDialogService);

  @ViewChild('cancelButton') private cancelButton?: ElementRef<HTMLButtonElement>;

  constructor() {
    effect(() => {
      const open = !!this.confirmDialog.current();
      document.body.style.overflow = open ? 'hidden' : '';
      if (open) {
        setTimeout(() => this.cancelButton?.nativeElement.focus());
      }
    });
  }

  accept(): void {
    this.confirmDialog.choose(true);
  }

  cancel(): void {
    this.confirmDialog.choose(false);
  }

  @HostListener('document:keydown.escape', ['$event'])
  onEscape(event: Event): void {
    if (!this.confirmDialog.current()) {
      return;
    }

    event.preventDefault();
    this.cancel();
  }
}
