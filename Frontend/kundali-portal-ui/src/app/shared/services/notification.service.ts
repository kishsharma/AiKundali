import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export type NotificationType = 'success' | 'error' | 'warning' | 'info';

export interface AppNotification {
  id: number;
  type: NotificationType;
  message: string;
  title?: string;
  durationMs: number;
}

export interface NotificationOptions {
  title?: string;
  durationMs?: number;
}

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private readonly maxVisible = 3;
  private readonly notificationsSubject = new BehaviorSubject<AppNotification[]>([]);
  private nextId = 1;
  private readonly timers = new Map<number, ReturnType<typeof setTimeout>>();

  readonly notifications$ = this.notificationsSubject.asObservable();

  success(message: string, options: NotificationOptions = {}): number {
    return this.show('success', message, options);
  }

  error(message: string, options: NotificationOptions = {}): number {
    return this.show('error', message, options);
  }

  warning(message: string, options: NotificationOptions = {}): number {
    return this.show('warning', message, options);
  }

  info(message: string, options: NotificationOptions = {}): number {
    return this.show('info', message, options);
  }

  show(type: NotificationType, message: string, options: NotificationOptions = {}): number {
    const trimmedMessage = message.trim();
    if (!trimmedMessage) {
      return 0;
    }

    const notification: AppNotification = {
      id: this.nextId++,
      type,
      message: trimmedMessage,
      title: options.title,
      durationMs: options.durationMs ?? this.getDefaultDuration(type)
    };

    const current = this.notificationsSubject.value;
    const next = [notification, ...current].slice(0, this.maxVisible);
    const removed = current.filter(item => !next.some(active => active.id === item.id));
    removed.forEach(item => this.clearTimer(item.id));
    this.notificationsSubject.next(next);

    if (notification.durationMs > 0) {
      this.timers.set(notification.id, setTimeout(() => this.dismiss(notification.id), notification.durationMs));
    }

    return notification.id;
  }

  dismiss(id: number): void {
    this.clearTimer(id);
    this.notificationsSubject.next(this.notificationsSubject.value.filter(item => item.id !== id));
  }

  clear(): void {
    this.timers.forEach((_, id) => this.clearTimer(id));
    this.notificationsSubject.next([]);
  }

  private getDefaultDuration(type: NotificationType): number {
    return type === 'success' || type === 'info' ? 4200 : 7600;
  }

  private clearTimer(id: number): void {
    const timer = this.timers.get(id);
    if (timer) {
      clearTimeout(timer);
      this.timers.delete(id);
    }
  }
}
