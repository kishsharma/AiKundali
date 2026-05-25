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
      background: #FFFFFF;
      border-radius: 1rem;
      box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);
      padding: 1.5rem;
      transition: all 300ms ease-in-out;
      border: 1px solid transparent;

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
      margin-bottom: 1.5rem;
      padding-bottom: 1rem;
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
      line-height: 1.6;
    }

    .card-footer {
      margin-top: 1.5rem;
      padding-top: 1rem;
      border-top: 1px solid #E0E0E0;
    }
  `],
})
export class SectionCardComponent {
  @Input() title: string = '';
  @Input() interactive: boolean = false;
  @Input() highlight: boolean = false;
  @Input() footer: boolean = false;
  @Input() headerAction: boolean = false;
}
