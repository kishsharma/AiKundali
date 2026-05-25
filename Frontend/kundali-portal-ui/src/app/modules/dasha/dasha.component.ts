// Dasha Module - Dasha Period Analysis Page
import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PageHeaderComponent } from '../../shared/components/page-header.component';
import { SectionCardComponent } from '../../shared/components/section-card.component';

@Component({
  selector: 'app-dasha',
  standalone: true,
  imports: [CommonModule, PageHeaderComponent, SectionCardComponent],
  templateUrl: './dasha.component.html',
  styles: [`
    .dasha-layout {
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

    @media (max-width: 768px) {
      .dasha-layout {
        grid-template-columns: 1fr;
      }
    }
  `],
})
export class DashaComponent {}
