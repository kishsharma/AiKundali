import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { PageHeaderComponent } from '../../shared/components/page-header.component';
import { NotificationService } from '../../shared/services/notification.service';
import { UserAnalysisPlanTile, UserAnalysisRow, UserAnalysisService } from '../../core/services/user-analysis.service';

@Component({
  selector: 'app-user-analysis',
  standalone: true,
  imports: [CommonModule, PageHeaderComponent],
  templateUrl: './user-analysis.component.html',
  styleUrl: './user-analysis.component.scss',
})
export class UserAnalysisComponent implements OnInit {
  private readonly analysis = inject(UserAnalysisService);
  private readonly notifications = inject(NotificationService);
  private readonly cdr = inject(ChangeDetectorRef);

  loading = false;
  errorMessage = '';
  plans: UserAnalysisPlanTile[] = [];
  users: UserAnalysisRow[] = [];
  selectedPlan = 'Free';

  ngOnInit(): void {
    this.load();
  }

  get selectedUsers(): UserAnalysisRow[] {
    return this.users.filter(user => user.planCode === this.selectedPlan);
  }

  get selectedPlanName(): string {
    return this.plans.find(plan => plan.code === this.selectedPlan)?.name || 'Free';
  }

  selectPlan(code: string): void {
    this.selectedPlan = code;
  }

  load(): void {
    this.loading = true;
    this.errorMessage = '';
    this.analysis.getAnalysis().subscribe({
      next: (response) => {
        this.plans = response?.plans ?? [];
        this.users = response?.users ?? [];
        if (!this.plans.some(plan => plan.code === this.selectedPlan) && this.plans.length) {
          this.selectedPlan = this.plans[0].code;
        }
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: (error) => {
        this.errorMessage = error?.error?.message || 'Unable to load user analysis.';
        this.notifications.error(this.errorMessage);
        this.loading = false;
        this.cdr.markForCheck();
      },
    });
  }

  questionsLeft(user: UserAnalysisRow): string {
    if (user.questionsDailyLimit == null) {
      return 'Unlimited';
    }

    return String(Math.max(0, user.questionsDailyLimit - user.questionsUsedToday));
  }

  questionsLimit(user: UserAnalysisRow): string {
    return user.questionsDailyLimit == null ? 'Unlimited' : String(user.questionsDailyLimit);
  }

  left(used: number, limit: number): number {
    return Math.max(0, limit - used);
  }

  languageLabel(code?: string | null): string {
    const value = (code || '').toLowerCase();
    if (value === 'hi') {
      return 'Hindi';
    }
    if (value === 'kn') {
      return 'Kannada';
    }
    if (value === 'en') {
      return 'English';
    }
    return code || 'English';
  }
}
