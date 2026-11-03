import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { NavigationEnd, Router } from '@angular/router';
import { EMPTY, filter, catchError } from 'rxjs';
import { environment } from '../../config/environment/environment';

export type TrackedUserFlow = 'KundaliGeneration' | 'CompatibilityMatch' | 'KundaliForm' | 'CompatibilityForm';

interface ActivityEvent {
  action: string;
  route: string;
  sessionId: string;
  flow?: TrackedUserFlow;
  operationId?: string;
  previousRoute?: string;
  durationMs?: number;
}

interface ActiveOperation {
  flow: TrackedUserFlow;
  route: string;
  startedAt: number;
}

interface UnsavedReport {
  route: string;
  operationId: string;
  generatedAt: number;
}

interface StartedForm {
  route: string;
  operationId: string;
  startedAt: number;
}

@Injectable({ providedIn: 'root' })
export class UserActivityTrackingService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly endpoint = `${environment.apiBaseUrl}/usage/activity-events`;
  private readonly sessionId = this.getSessionId();
  private readonly operations = new Map<string, ActiveOperation>();
  private readonly unsavedReports = new Map<TrackedUserFlow, UnsavedReport>();
  private readonly startedForms = new Map<TrackedUserFlow, StartedForm>();

  private currentRoute = '';
  private activeSince: number | null = null;

  constructor() {
    this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe(event => this.onNavigation(event.urlAfterRedirects));

    if (typeof document !== 'undefined' && typeof window !== 'undefined') {
      document.addEventListener('visibilitychange', this.onVisibilityChange);
      document.addEventListener('focusin', this.onFormFocus);
      window.addEventListener('pagehide', this.onPageHide);
      window.setInterval(() => this.flushActiveTime(), 60_000);
    }
  }

  startOperation(flow: TrackedUserFlow): string {
    this.recordUnsavedReport(flow);
    const formFlow = this.formFlowFor(flow);
    if (formFlow) {
      this.startedForms.delete(formFlow);
    }
    const operationId = this.createId();
    const operation = {
      flow,
      route: this.currentRoute || this.routeFromUrl(this.router.url),
      startedAt: performance.now()
    };
    this.operations.set(operationId, operation);
    this.record('OperationStarted', operation.route, { flow, operationId });
    return operationId;
  }

  finishOperation(operationId: string, succeeded: boolean): void {
    const operation = this.operations.get(operationId);
    if (!operation) {
      return;
    }

    this.operations.delete(operationId);
    const durationMs = Math.max(0, Math.round(performance.now() - operation.startedAt));
    this.record(succeeded ? 'OperationSucceeded' : 'OperationFailed', operation.route, {
      flow: operation.flow,
      operationId,
      durationMs
    });

    if (succeeded) {
      this.unsavedReports.set(operation.flow, {
        route: operation.route,
        operationId,
        generatedAt: performance.now()
      });
    }
  }

  markReportSaved(flow: TrackedUserFlow): void {
    this.unsavedReports.delete(flow);
  }

  private readonly onNavigation = (url: string): void => {
    const nextRoute = this.routeFromUrl(url);
    if (!nextRoute || nextRoute === this.currentRoute) {
      return;
    }

    const previousRoute = this.currentRoute || undefined;
    if (previousRoute) {
      this.flushActiveTime();
      this.abandonOperationsForRoute(previousRoute);
      this.abandonFormsForRoute(previousRoute);
      this.recordUnsavedReportsForRoute(previousRoute);
    }

    this.currentRoute = nextRoute;
    this.activeSince = this.isPageVisible() ? performance.now() : null;
    this.record('PageViewed', nextRoute, { previousRoute });
  };

  private readonly onVisibilityChange = (): void => {
    if (this.isPageVisible()) {
      this.activeSince = performance.now();
      return;
    }

    this.flushActiveTime();
    this.activeSince = null;
  };

  private readonly onPageHide = (): void => {
    this.flushActiveTime();
    if (this.currentRoute) {
      this.abandonOperationsForRoute(this.currentRoute);
      this.abandonFormsForRoute(this.currentRoute);
      this.recordUnsavedReportsForRoute(this.currentRoute);
    }
  };

  private readonly onFormFocus = (event: FocusEvent): void => {
    const target = event.target;
    if (!(target instanceof HTMLElement)
      || !target.closest('input, select, textarea, [contenteditable="true"]')) {
      return;
    }

    const flow = this.formFlowForRoute(this.currentRoute);
    if (!flow || this.startedForms.has(flow)) {
      return;
    }

    const operationId = this.createId();
    this.startedForms.set(flow, { route: this.currentRoute, operationId, startedAt: performance.now() });
    this.record('FormStarted', this.currentRoute, { flow, operationId });
  };

  private flushActiveTime(): void {
    if (!this.currentRoute || !this.isPageVisible() || this.activeSince === null) {
      return;
    }

    const now = performance.now();
    const durationMs = Math.max(0, Math.round(now - this.activeSince));
    this.activeSince = now;
    if (durationMs >= 1_000) {
      this.record('PageVisibleTime', this.currentRoute, { durationMs });
    }
  }

  private recordUnsavedReportsForRoute(route: string): void {
    for (const [flow, report] of this.unsavedReports) {
      if (report.route !== route) {
        continue;
      }
      this.recordUnsavedReport(flow);
    }
  }

  private recordUnsavedReport(flow: TrackedUserFlow): void {
    const report = this.unsavedReports.get(flow);
    if (!report) {
      return;
    }

    this.record('ReportGeneratedNotSaved', report.route, {
      flow,
      operationId: report.operationId,
      durationMs: Math.max(0, Math.round(performance.now() - report.generatedAt))
    });
    this.unsavedReports.delete(flow);
  }

  private abandonOperationsForRoute(route: string): void {
    for (const [operationId, operation] of this.operations) {
      if (operation.route !== route) {
        continue;
      }

      this.record('OperationAbandoned', route, {
        flow: operation.flow,
        operationId,
        durationMs: Math.max(0, Math.round(performance.now() - operation.startedAt))
      });
      this.operations.delete(operationId);
    }
  }

  private abandonFormsForRoute(route: string): void {
    for (const [flow, form] of this.startedForms) {
      if (form.route !== route) {
        continue;
      }
      this.record('FormAbandoned', route, {
        flow,
        operationId: form.operationId,
        durationMs: Math.max(0, Math.round(performance.now() - form.startedAt))
      });
      this.startedForms.delete(flow);
    }
  }

  private formFlowForRoute(route: string): TrackedUserFlow | null {
    if (route.includes('/kundali')) return 'KundaliForm';
    if (route.includes('/compatibility')) return 'CompatibilityForm';
    return null;
  }

  private formFlowFor(operationFlow: TrackedUserFlow): TrackedUserFlow | null {
    if (operationFlow === 'KundaliGeneration') return 'KundaliForm';
    if (operationFlow === 'CompatibilityMatch') return 'CompatibilityForm';
    return null;
  }

  private record(action: string, route: string, extra: Partial<ActivityEvent> = {}): void {
    if (!route) {
      return;
    }

    const event: ActivityEvent = {
      action,
      route,
      sessionId: this.sessionId,
      ...extra
    };

    this.http.post(this.endpoint, event).pipe(catchError(() => EMPTY)).subscribe();
  }

  private routeFromUrl(url: string): string {
    const path = url.split(/[?#]/, 1)[0] || '/';
    return path
      .replace(/(?<=\/)[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}(?=\/|$)/gi, ':id')
      .replace(/(?<=\/)\d+(?=\/|$)/g, ':id')
      .replace(/\/$/, '') || '/';
  }

  private isPageVisible(): boolean {
    return typeof document === 'undefined' || document.visibilityState === 'visible';
  }

  private getSessionId(): string {
    try {
      const key = 'horalife-activity-session';
      const existing = sessionStorage.getItem(key);
      if (existing && /^[0-9a-f-]{36}$/i.test(existing)) {
        return existing;
      }
      const id = this.createId();
      sessionStorage.setItem(key, id);
      return id;
    } catch {
      return this.createId();
    }
  }

  private createId(): string {
    if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
      return crypto.randomUUID();
    }
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, token => {
      const random = Math.random() * 16 | 0;
      return (token === 'x' ? random : (random & 0x3 | 0x8)).toString(16);
    });
  }
}
