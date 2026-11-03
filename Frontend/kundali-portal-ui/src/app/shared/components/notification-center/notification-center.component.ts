import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import { interval, Subject, takeUntil } from 'rxjs';
import { PractitionerCrmService, CrmNotification } from '../../../core/services/practitioner-crm.service';

@Component({
  selector: 'app-notification-center',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="notification-center">
      <button type="button" class="bell" (click)="toggle()" aria-label="Notifications" [attr.aria-expanded]="isOpen" title="Notifications">
        <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M18 8a6 6 0 0 0-12 0c0 7-3 9-3 9h18s-3-2-3-9"></path><path d="M10 21h4"></path></svg>
        <span class="unread-count" *ngIf="unreadCount">{{ unreadCount > 9 ? '9+' : unreadCount }}</span>
      </button>
      <section class="popover" *ngIf="isOpen" aria-label="Notifications" role="dialog">
        <header><strong>Notifications</strong><button type="button" (click)="markAllRead()" [disabled]="!unreadCount">Mark all read</button></header>
        <p class="state" *ngIf="loading && !notifications.length">Loading notifications…</p>
        <div class="state error" *ngIf="loadError"><span>{{ loadError }}</span><button type="button" (click)="refresh()">Retry</button></div>
        <p class="state" *ngIf="!loading && !loadError && !notifications.length">No consultation reminders yet.</p>
        <button type="button" class="notification" *ngFor="let item of notifications" [class.unread]="!item.isRead" (click)="open(item)">
          <span class="dot" *ngIf="!item.isRead"></span><span class="copy"><strong>{{ item.title }}</strong><span>{{ item.message }}</span><small>{{ item.createdAt | date:'dd-MMM-yyyy, hh:mm a':'+0530' }} IST</small></span>
        </button>
      </section>
    </div>
  `,
  styles: [`
    :host{display:block}.notification-center{position:relative}.bell{position:relative;display:grid;place-items:center;width:40px;height:40px;border:1px solid var(--theme-line);border-radius:12px;background:var(--theme-surface);color:var(--theme-text);cursor:pointer}.bell svg{width:19px;height:19px;fill:none;stroke:currentColor;stroke-width:1.8;stroke-linecap:round;stroke-linejoin:round}.unread-count{position:absolute;top:-5px;right:-5px;display:grid;place-items:center;min-width:17px;height:17px;padding:0 4px;border-radius:999px;background:var(--theme-danger,#d14b55);color:white;font-size:10px;font-weight:800}.popover{position:absolute;top:48px;right:0;z-index:2200;width:min(370px,calc(100vw - 28px));max-height:min(70vh,520px);overflow:auto;border:1px solid var(--theme-line);border-radius:16px;background:var(--theme-surface);color:var(--theme-text);box-shadow:0 18px 50px rgba(0,0,0,.22)}header{position:sticky;top:0;display:flex;align-items:center;justify-content:space-between;padding:14px 16px;border-bottom:1px solid var(--theme-line);background:var(--theme-surface)}header button{border:0;background:none;color:var(--theme-accent);font:inherit;font-size:12px;font-weight:700;cursor:pointer}header button:disabled{opacity:.45;cursor:default}.notification{position:relative;display:flex;gap:10px;width:100%;padding:13px 16px;border:0;border-bottom:1px solid var(--theme-line);background:transparent;color:inherit;text-align:left;cursor:pointer}.notification:hover,.notification.unread{background:color-mix(in srgb,var(--theme-accent) 7%,var(--theme-surface))}.copy{display:grid;gap:5px}.copy>span{font-size:12px;line-height:1.45}.copy small,.state{color:var(--theme-muted,#778096);font-size:11px}.dot{width:8px;height:8px;flex:0 0 8px;margin-top:5px;border-radius:50%;background:var(--theme-accent)}.state{padding:18px;margin:0}.state.error{display:flex;align-items:center;justify-content:space-between;gap:12px;color:var(--theme-danger,#d14b55)}.state button{border:0;background:none;color:var(--theme-accent);font:inherit;font-weight:700;cursor:pointer}@media(max-width:600px){.popover{right:-55px}}
  `]
})
export class NotificationCenterComponent implements OnInit, OnDestroy {
  private readonly crm = inject(PractitionerCrmService);
  private readonly router = inject(Router);
  private readonly destroy$ = new Subject<void>();
  notifications: CrmNotification[] = [];
  isOpen = false;
  loading = false;
  loadError = '';

  get unreadCount(): number { return this.notifications.filter(item => !item.isRead).length; }

  ngOnInit(): void {
    this.refresh();
    interval(60_000).pipe(takeUntil(this.destroy$)).subscribe(() => this.refresh());
  }

  ngOnDestroy(): void { this.destroy$.next(); this.destroy$.complete(); }

  toggle(): void { this.isOpen = !this.isOpen; if (this.isOpen) this.refresh(); }

  open(item: CrmNotification): void {
    if (!item.isRead) {
      item.isRead = true;
      this.crm.markNotificationRead(item.id).subscribe({
        error: () => { item.isRead = false; this.loadError = 'Unable to mark this reminder as read.'; }
      });
    }
    this.isOpen = false;
    this.router.navigateByUrl(item.link);
  }

  markAllRead(): void {
    if (!this.unreadCount) return;
    this.crm.markAllNotificationsRead().subscribe({
      next: () => { this.notifications = this.notifications.map(item => ({ ...item, isRead: true })); this.loadError = ''; },
      error: () => this.loadError = 'Unable to mark reminders as read. Please try again.'
    });
  }

  refresh(): void {
    this.loading = true;
    this.crm.getNotifications().subscribe({
      next: items => { this.notifications = items; this.loading = false; this.loadError = ''; },
      error: () => { this.loading = false; this.loadError = 'Unable to load notifications. Check your connection and retry.'; }
    });
  }
}
