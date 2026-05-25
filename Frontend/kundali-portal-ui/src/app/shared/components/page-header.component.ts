// Page Header Component - consistent page title & breadcrumbs
import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

export interface Breadcrumb {
  label: string;
  routerLink?: string;
  active?: boolean;
}

@Component({
  selector: 'app-page-header',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="page-header">
      <div class="header-content">
        <div class="header-top">
          <nav *ngIf="breadcrumbs && breadcrumbs.length > 0" class="breadcrumbs" aria-label="breadcrumb">
            <ol>
              <li *ngFor="let crumb of breadcrumbs; let last = last">
                <a *ngIf="!last && crumb.routerLink" [routerLink]="crumb.routerLink" class="breadcrumb-link">
                  {{ crumb.label }}
                </a>
                <span *ngIf="last || !crumb.routerLink" class="breadcrumb-text" [class.active]="last">
                  {{ crumb.label }}
                </span>
              </li>
            </ol>
          </nav>
        </div>

        <div class="header-main">
          <div class="title-section">
            <h1 class="page-title">{{ title }}</h1>
            <p *ngIf="subtitle" class="page-subtitle">{{ subtitle }}</p>
          </div>

          <div *ngIf="showActions" class="header-actions">
            <ng-content select="[page-actions]"></ng-content>
          </div>
        </div>

        <div *ngIf="description" class="header-description">
          {{ description }}
        </div>
      </div>
    </div>
  `,
  styles: [`
    .page-header {
      background: linear-gradient(135deg, rgba(126, 59, 168, 0.05), rgba(255, 30, 100, 0.03));
      border-bottom: 1px solid #E0E0E0;
      padding: 1.5rem 2rem;
      margin-bottom: 2rem;
    }

    .header-content {
      display: flex;
      flex-direction: column;
      gap: 1rem;
    }

    .header-top {
      display: flex;
      align-items: center;
    }

    .breadcrumbs ol {
      display: flex;
      flex-wrap: wrap;
      gap: 0.5rem;
      margin: 0;
      padding: 0;
      list-style: none;
    }

    .breadcrumbs li {
      display: flex;
      align-items: center;

      &:not(:last-child)::after {
        content: '/';
        margin-left: 0.5rem;
        color: #BDBDBD;
      }
    }

    .breadcrumb-link {
      color: #0097A7;
      text-decoration: none;
      font-size: 0.875rem;
      transition: color 300ms ease;

      &:hover {
        color: #00BCD4;
        text-decoration: underline;
      }
    }

    .breadcrumb-text {
      color: #757575;
      font-size: 0.875rem;

      &.active {
        color: #212121;
        font-weight: 500;
      }
    }

    .header-main {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      gap: 2rem;
    }

    .title-section {
      flex: 1;
    }

    .page-title {
      margin: 0;
      font-size: 2rem;
      font-weight: 600;
      color: #212121;
    }

    .page-subtitle {
      margin: 0.5rem 0 0 0;
      font-size: 0.875rem;
      color: #757575;
      font-weight: 500;
    }

    .header-actions {
      display: flex;
      gap: 0.5rem;
      flex-wrap: wrap;
    }

    .header-description {
      font-size: 0.875rem;
      color: #424242;
      line-height: 1.5;
      max-width: 600px;
    }

    @media (max-width: 768px) {
      .page-header {
        padding: 1rem;
        margin-bottom: 1.5rem;
      }

      .header-main {
        flex-direction: column;
        align-items: stretch;
      }

      .page-title {
        font-size: 1.5rem;
      }
    }
  `],
})
export class PageHeaderComponent {
  @Input() title: string = '';
  @Input() subtitle: string = '';
  @Input() description: string = '';
  @Input() breadcrumbs: Breadcrumb[] = [];
  @Input() showActions: boolean = false;
}
