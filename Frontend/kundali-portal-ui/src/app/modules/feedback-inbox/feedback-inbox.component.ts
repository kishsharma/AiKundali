import { CommonModule, DatePipe } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { ProductFeedbackItem, ProductFeedbackService } from '../../core/services/product-feedback.service';
import { PageHeaderComponent } from '../../shared/components/page-header.component';

@Component({
  selector: 'app-feedback-inbox',
  standalone: true,
  imports: [CommonModule, DatePipe, PageHeaderComponent],
  templateUrl: './feedback-inbox.component.html',
  styleUrl: './feedback-inbox.component.scss',
})
export class FeedbackInboxComponent implements OnInit {
  private readonly feedback = inject(ProductFeedbackService);
  private readonly changeDetector = inject(ChangeDetectorRef);

  rows: ProductFeedbackItem[] = [];
  loading = true;
  errorMessage = '';

  ngOnInit(): void {
    this.feedback.list().subscribe({
      next: (rows) => {
        this.rows = rows ?? [];
        this.loading = false;
        this.changeDetector.markForCheck();
      },
      error: () => {
        this.loading = false;
        this.errorMessage = 'Unable to load feedback.';
        this.changeDetector.markForCheck();
      },
    });
  }

  sentimentLabel(value: string): string {
    return value === 'needs_work' ? 'Needs work' : 'Useful';
  }
}
