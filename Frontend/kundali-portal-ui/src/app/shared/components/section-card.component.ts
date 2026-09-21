// Section Card Component - reusable card container
import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-section-card',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="card" [class.interactive]="interactive" [class.highlight]="highlight">
      <div *ngIf="title" class="card-header">
        <h3 class="card-title">{{ title }}</h3>
        <div *ngIf="headerAction" class="card-action">
          <ng-content select="[card-action]"></ng-content>
        </div>
      </div>
      <div class="card-content">
        <ng-content></ng-content>
      </div>
      <div *ngIf="footer" class="card-footer">
        <ng-content select="[card-footer]"></ng-content>
      </div>
    </div>
  `,
  styles: [`
    .card {
      display: block;
      width: 100%;
      max-width: none;
      min-width: 0;
      background: #FFFFFF;
      padding: 0.3rem;
      // border-radius: 1rem;
      // box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);
      padding: 0.3rem;
      transition: all 300ms ease-in-out;
      // border: 1px solid transparent;

      &.interactive {
        cursor: pointer;

        &:hover {
          box-shadow: 0 10px 15px -3px rgba(0, 0, 0, 0.1);
          transform: translateY(-2px);
        }
      }

      &.highlight {
        border-color: #7E3BA8;
        background: linear-gradient(135deg, rgba(126, 59, 168, 0.02), rgba(255, 30, 100, 0.02));
      }
    }

    .card-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 0.85rem;
      padding-bottom: 0.75rem;
      border-bottom: 1px solid #E0E0E0;
    }

    .card-title {
      margin: 0;
      font-size: 1.125rem;
      font-weight: 600;
      color: #212121;
    }

    .card-content {
      color: #424242;
      line-height: 1.5;
    }

    .card-footer {
      margin-top: 0.85rem;
      padding-top: 0.75rem;
      border-top: 1px solid #E0E0E0;
    }

    /* Runtime theme card */
    :host {
      display: block;
      width: 100%;
      max-width: none;
      min-width: 0;
      color: var(--theme-text);
    }

    .card,
    .card-content {
      width: 100%;
      max-width: none;
      min-width: 0;
    }

    .card {
      border: 1px solid var(--theme-line);
      border-radius: var(--theme-radius-card);
      background: var(--theme-surface-soft, var(--theme-surface));
      color: var(--theme-text);
      box-shadow: 0 18px 44px var(--theme-shadow);
    }

    .card.interactive:hover {
      border-color: color-mix(in srgb, var(--theme-accent) 42%, transparent);
      box-shadow: 0 20px 50px var(--theme-shadow);
    }

    .card.highlight {
      border-color: var(--theme-accent);
      background: color-mix(in srgb, var(--theme-accent) 8%, var(--theme-surface));
    }

    .card-header,
    .card-footer {
      border-color: var(--theme-line-soft);
    }

    .card-title {
      color: var(--theme-text);
    }

    .card-content {
      color: var(--theme-text-muted);
    }  `],
})
export class SectionCardComponent {
  @Input() title: string = '';
  @Input() interactive: boolean = false;
  @Input() highlight: boolean = false;
  @Input() footer: boolean = false;
  @Input() headerAction: boolean = false;
}
