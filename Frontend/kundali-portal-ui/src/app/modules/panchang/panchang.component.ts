// Panchang Module - Panchang Viewer Page
import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { PageHeaderComponent } from '../../shared/components/page-header.component';
import { SectionCardComponent } from '../../shared/components/section-card.component';

@Component({
  selector: 'app-panchang',
  standalone: true,
  imports: [CommonModule, RouterModule, PageHeaderComponent, SectionCardComponent],
  templateUrl: './panchang.component.html',
  styles: [`
    .panchang-layout {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(400px, 1fr));
      gap: 2rem;
    }

    .coming-soon {
      text-align: center;
      padding: 3rem 2rem;
      background: linear-gradient(135deg, rgba(126, 59, 168, 0.05), rgba(255, 30, 100, 0.05));
      border-radius: 1rem;
      color: #424242;

      p {
        margin: 0.5rem 0;
        font-size: 1rem;

        &.subtitle {
          font-size: 0.875rem;
          color: #757575;
        }
      }
    }

    .muhurtha-link {
      display: inline-flex;
      margin-top: 1rem;
      padding: 0.75rem 1rem;
      border-radius: 0.5rem;
      background: #7E3BA8;
      color: #fff;
      font-weight: 700;
      text-decoration: none;
    }

    @media (max-width: 768px) {
      .panchang-layout {
        grid-template-columns: 1fr;
      }
    }
  `],
})
export class PanchangComponent {}

