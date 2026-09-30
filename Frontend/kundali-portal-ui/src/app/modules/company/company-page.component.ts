import { NgFor, NgIf } from '@angular/common';
import { ChangeDetectorRef, Component, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';
import { business } from '../../config/business.config';
import { SiteFooterComponent } from '../../shared/components/site-footer/site-footer.component';
import { CompanyPageContent, companyPages } from './company-page.content';

@Component({
  selector: 'app-company-page',
  standalone: true,
  imports: [NgFor, NgIf, SiteFooterComponent],
  templateUrl: './company-page.component.html',
  styleUrl: './company-page.component.scss',
})
export class CompanyPageComponent {
  readonly business = business;
  page: CompanyPageContent = companyPages['about'];

  private readonly route = inject(ActivatedRoute);
  private readonly cdr = inject(ChangeDetectorRef);

  constructor() {
    const destroyRef = inject(DestroyRef);
    this.route.data.pipe(takeUntilDestroyed(destroyRef)).subscribe((data) => {
      const key = String(data['page'] ?? '');
      this.page = companyPages[key] ?? companyPages['about'];
      this.cdr.markForCheck();
    });
  }
}
