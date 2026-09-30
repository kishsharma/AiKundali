import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PageHeaderComponent } from '../../shared/components/page-header.component';
import { NotificationService } from '../../shared/services/notification.service';
import {
  AuditLogItem,
  AuditLogsService,
  CommunicationLogItem,
  LogPage,
  LogQuery
} from '../../core/services/audit-logs.service';

type LogTab = 'audit' | 'communication';

@Component({
  selector: 'app-audit-logs',
  standalone: true,
  imports: [CommonModule, FormsModule, PageHeaderComponent],
  templateUrl: './audit-logs.component.html',
  styleUrl: './audit-logs.component.scss'
})
export class AuditLogsComponent implements OnInit {
  private readonly auditLogsService = inject(AuditLogsService);
  private readonly notifications = inject(NotificationService);

  activeTab: LogTab = 'audit';
  loading = false;
  errorMessage = '';

  auditLogs: AuditLogItem[] = [];
  communicationLogs: CommunicationLogItem[] = [];
  total = 0;
  page = 1;
  pageSize = 25;

  filters = {
    dateFrom: '',
    dateTo: '',
    action: '',
    category: '',
    channel: '',
    status: '',
    messageType: '',
    recipient: ''
  };

  readonly actionGuide: { action: string; description: string }[] = [
    { action: 'LoginSuccess', description: 'A user signed in with email and password.' },
    { action: 'LoginFailed', description: 'A sign-in with email and password did not succeed.' },
    { action: 'OtpLoginRequested', description: 'A sign-in code was sent to the user\'s email.' },
    { action: 'OtpLoginRequestFailed', description: 'Sending the sign-in code did not succeed.' },
    { action: 'OtpLoginSuccess', description: 'A user signed in with an email code.' },
    { action: 'OtpLoginFailed', description: 'Sign-in with an email code did not succeed.' },
    { action: 'UserRegistered', description: 'A new account was created.' },
    { action: 'UserRegistrationFailed', description: 'Creating an account did not succeed.' },
    { action: 'OtpRegistrationRequested', description: 'A registration code was sent to the email.' },
    { action: 'OtpRegistrationRequestFailed', description: 'Sending the registration code did not succeed.' },
    { action: 'OtpRegistrationVerified', description: 'The registration email code was accepted.' },
    { action: 'OtpRegistrationVerificationFailed', description: 'The registration email code was not accepted.' },
    { action: 'OtpUserRegistered', description: 'A new account was created after the email code was verified.' },
    { action: 'PasswordChanged', description: 'The user changed their password.' },
    { action: 'PasswordChangeFailed', description: 'The password change did not succeed.' },
    { action: 'StoreHandoffIssued', description: 'The portal prepared a one-time sign-in for the store.' },
    { action: 'StoreAutoLogin', description: 'The same signed-in user opened the store.' },
    { action: 'StoreAutoLoginFailed', description: 'Opening the store from the portal did not succeed.' },
    { action: 'KundaliCreated', description: 'A kundali was saved.' },
    { action: 'KundaliDeleted', description: 'A saved kundali was deleted.' },
    { action: 'CompatibilitySaved', description: 'A compatibility match was saved.' },
    { action: 'CompatibilityDeleted', description: 'A saved compatibility match was deleted.' },
    { action: 'PdfExported', description: 'A report PDF was downloaded.' },
    { action: 'PdfExportFailed', description: 'The report PDF download did not succeed.' },
    { action: 'UserPreferredLanguageChanged', description: 'The user\'s language was changed.' },
    { action: 'UserKundaliSaveLimitChanged', description: 'An admin changed how many kundalis the user can save.' },
    { action: 'UserPremiumPlanChanged', description: 'An admin changed the user\'s plan.' },
    { action: 'UserActiveStatusChanged', description: 'An admin turned the user account on or off.' }
  ];

  readonly auditActions = ['', ...this.actionGuide.map((item) => item.action)];

  readonly auditCategories = ['', 'Authentication', 'Security', 'DataChange', 'Export', 'AdminAction'];
  readonly communicationChannels = ['', 'Email', 'WhatsApp', 'SMS'];
  readonly communicationTypes = ['', 'OTP', 'Report', 'Receipt', 'Reminder', 'General'];
  readonly statuses = ['', 'Success', 'Failed', 'Sent', 'Queued'];
  private readonly cdr = inject(ChangeDetectorRef);
  ngOnInit(): void {
    this.loadLogs();
  }

  get firstItem(): number {
    return this.total === 0 ? 0 : (this.page - 1) * this.pageSize + 1;
  }

  get lastItem(): number {
    return Math.min(this.page * this.pageSize, this.total);
  }

  get canGoPrevious(): boolean {
    return this.page > 1 && !this.loading;
  }

  get canGoNext(): boolean {
    return this.page * this.pageSize < this.total && !this.loading;
  }

  setTab(tab: LogTab): void {
    if (this.activeTab === tab) {
      return;
    }

    this.activeTab = tab;
    this.page = 1;
    this.errorMessage = '';
    this.loadLogs();
  }

  applyFilters(): void {
    this.page = 1;
    this.loadLogs();
  }

  resetFilters(): void {
    this.filters = {
      dateFrom: '',
      dateTo: '',
      action: '',
      category: '',
      channel: '',
      status: '',
      messageType: '',
      recipient: ''
    };
    this.page = 1;
    this.loadLogs();
  }

  previousPage(): void {
    if (this.canGoPrevious) {
      this.page -= 1;
      this.loadLogs();
    }
  }

  nextPage(): void {
    if (this.canGoNext) {
      this.page += 1;
      this.loadLogs();
    }
  }

  loadLogs(): void {
    this.loading = true;
    this.errorMessage = '';

    const query = this.buildQuery();

    if (this.activeTab === 'audit') {
      this.auditLogsService.getAuditLogs(query).subscribe({
        next: (page: LogPage<AuditLogItem>) => this.handlePage(page),
        
        error: (error: any) => this.handleLoadError(error)
      });
      return;
    }

    this.auditLogsService.getCommunicationLogs(query).subscribe({
      next: (page: LogPage<CommunicationLogItem>) => this.handlePage(page),
      error: (error: any) => this.handleLoadError(error)
    });
  }

  formatDate(value: string): string {
    if (!value) {
      return '-';
    }

    return new Date(value).toLocaleString();
  }

  trackById(_: number, item: AuditLogItem | CommunicationLogItem): string {
    return item.id;
  }

  private handleLoadError(error: any): void {
    this.errorMessage = error?.error?.message || error?.message || 'Unable to load logs.';
    this.notifications.error(this.errorMessage);
    this.loading = false;
    this.cdr.detectChanges();
  }

  private handlePage(page: LogPage<AuditLogItem> | LogPage<CommunicationLogItem>): void {
    this.total = page?.total ?? 0;
    this.page = page?.page ?? this.page;
    this.pageSize = page?.pageSize ?? this.pageSize;

    if (this.activeTab === 'audit') {
      this.auditLogs = (page?.items ?? []) as AuditLogItem[];
    } else {
      this.communicationLogs = (page?.items ?? []) as CommunicationLogItem[];
    }

    this.loading = false;
    this.cdr.detectChanges();
  }

  private buildQuery(): LogQuery {
    const query: LogQuery = {
      dateFrom: this.filters.dateFrom ? `${this.filters.dateFrom}T00:00:00Z` : undefined,
      dateTo: this.filters.dateTo ? `${this.filters.dateTo}T23:59:59Z` : undefined,
      status: this.filters.status,
      page: this.page,
      pageSize: this.pageSize
    };

    if (this.activeTab === 'audit') {
      query.action = this.filters.action;
      query.category = this.filters.category;
    } else {
      query.channel = this.filters.channel;
      query.messageType = this.filters.messageType;
      query.recipient = this.filters.recipient;
    }

    return query;
  }
}


