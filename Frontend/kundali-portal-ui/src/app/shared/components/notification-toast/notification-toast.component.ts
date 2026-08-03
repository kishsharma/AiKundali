import { AsyncPipe, NgClass, NgFor, NgIf } from '@angular/common';
import { Component, inject } from '@angular/core';
import { AppNotification, NotificationService } from '../../services/notification.service';

@Component({
  selector: 'app-notification-toast',
  standalone: true,
  imports: [AsyncPipe, NgClass, NgFor, NgIf],
  templateUrl: './notification-toast.component.html',
  styleUrl: './notification-toast.component.scss'
})
export class NotificationToastComponent {
  private readonly notifications = inject(NotificationService);
  readonly notifications$ = this.notifications.notifications$;

  dismiss(id: number): void {
    this.notifications.dismiss(id);
  }

  getAriaRole(notification: AppNotification): 'alert' | 'status' {
    return notification.type === 'error' || notification.type === 'warning' ? 'alert' : 'status';
  }

  getIconPath(type: AppNotification['type']): string {
    switch (type) {
      case 'success':
        return 'M20 6 9 17l-5-5';
      case 'error':
        return 'M18 6 6 18M6 6l12 12';
      case 'warning':
        return 'M12 8v5M12 17h.01';
      default:
        return 'M12 16v-4M12 8h.01';
    }
  }
}
