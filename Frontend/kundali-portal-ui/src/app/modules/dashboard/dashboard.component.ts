// Dashboard Page Component - main landing page
import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PageHeaderComponent } from '../../shared/components/page-header.component';
import { SectionCardComponent } from '../../shared/components/section-card.component';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, PageHeaderComponent, SectionCardComponent],
  templateUrl: './dashboard.component.html',
  styles: [`
    .dashboard-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
      gap: 2rem;
    }

    .stats-row {
      grid-column: 1 / -1;
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 1.5rem;
    }

    .stat-content {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 0.5rem;
      padding: 1rem 0;
    }

    .stat-number {
      font-size: 2.5rem;
      font-weight: 700;
      color: #7E3BA8;
    }

    .stat-label {
      font-size: 0.875rem;
      color: #757575;
      text-align: center;
    }

    .empty-state {
      text-align: center;
      padding: 2rem;
      color: #757575;
    }

    .actions-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(120px, 1fr));
      gap: 1rem;
    }

    .action-card {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 0.75rem;
      padding: 1.5rem 1rem;
      background: linear-gradient(135deg, rgba(126, 59, 168, 0.05), rgba(255, 30, 100, 0.05));
      border: 2px solid transparent;
      border-radius: 0.75rem;
      cursor: pointer;
      transition: all 300ms ease;
      font-size: 0;

      &:hover {
        border-color: #7E3BA8;
        background: linear-gradient(135deg, rgba(126, 59, 168, 0.1), rgba(255, 30, 100, 0.1));
        transform: translateY(-2px);
      }
    }

    .action-icon {
      font-size: 2rem;
    }

    .action-label {
      font-size: 0.75rem;
      font-weight: 600;
      text-align: center;
      color: #212121;
    }

    @media (max-width: 768px) {
      .dashboard-grid {
        grid-template-columns: 1fr;
        gap: 1.5rem;
      }

      .actions-grid {
        grid-template-columns: repeat(2, 1fr);
      }
    }
  `],
})
export class DashboardComponent {}
