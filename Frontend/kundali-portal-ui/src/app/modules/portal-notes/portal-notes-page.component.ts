import { CommonModule } from '@angular/common';
import { Component, OnInit, ViewChild, inject } from '@angular/core';
import { RouterModule } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { PortalNote, PortalNotesService } from '../../core/services/portal-notes.service';
import { PageHeaderComponent } from '../../shared/components/page-header.component';
import { PortalNotesComponent } from '../../shared/components/portal-notes/portal-notes.component';
import { NotificationService } from '../../shared/services/notification.service';
import { AuthService } from '../../core/auth/auth.service';
import { isPaidUser } from '../../core/auth/plan.util';
import { SubscriptionModalService } from '../../core/services/subscription-modal.service';
import { ConfirmDialogService } from '../../shared/components/confirm-dialog/confirm-dialog.service';

@Component({
  selector: 'app-portal-notes-page',
  standalone: true,
  imports: [CommonModule, RouterModule, TranslocoPipe, PageHeaderComponent, PortalNotesComponent],
  templateUrl: './portal-notes-page.component.html',
  styleUrl: './portal-notes-page.component.scss'
})
export class PortalNotesPageComponent implements OnInit {
  @ViewChild(PortalNotesComponent) private notesPopup?: PortalNotesComponent;

  private readonly notesService = inject(PortalNotesService);
  private readonly notifications = inject(NotificationService);
  private readonly transloco = inject(TranslocoService);
  private readonly authService = inject(AuthService);
  private readonly subscriptionModal = inject(SubscriptionModalService);
  private readonly confirmDialog = inject(ConfirmDialogService);

  get isPremiumUser(): boolean {
    return isPaidUser(this.authService.getCurrentUser());
  }

  notes: PortalNote[] = [];
  loading = false;
  deletingContextKey = '';
  errorMessage = '';
  editingNote: PortalNote | null = null;
  isEditorOpen = false;

  ngOnInit(): void {
    if (!this.isPremiumUser) {
      return;
    }

    this.loadNotes();
  }

  unlockNotes(): void {
    this.subscriptionModal.open();
  }

  loadNotes(): void {
    this.loading = true;
    this.errorMessage = '';
    this.notesService.getAllNotes().subscribe({
      next: (notes) => {
        this.notes = notes || [];
        this.loading = false;
      },
      error: () => {
        this.errorMessage = this.t('portalNotes.loadError');
        this.loading = false;
      }
    });
  }

  openEditor(note: PortalNote): void {
    this.editingNote = note;
    this.isEditorOpen = true;
  }

  closeEditor(): void {
    this.isEditorOpen = false;
    this.loadNotes();
  }

  async deleteNote(note: PortalNote): Promise<void> {
    const key = this.contextKey(note);
    if (this.deletingContextKey) {
      return;
    }

    const confirmed = await this.confirmDialog.confirm({
      title: this.t('confirm.deleteNoteTitle'),
      message: this.t('portalNotes.confirmDelete'),
      confirmLabel: this.t('confirm.delete'),
      cancelLabel: this.t('confirm.cancel'),
      tone: 'danger',
    });
    if (!confirmed) {
      return;
    }

    this.deletingContextKey = key;
    this.notesService.deleteNote(note.contextType, note.contextId || null).subscribe({
      next: () => {
        this.notes = this.notes.filter((item) => this.contextKey(item) !== key);
        this.deletingContextKey = '';
        this.notifications.success(this.t('portalNotes.deleted'));
      },
      error: () => {
        this.deletingContextKey = '';
        this.notifications.error(this.t('portalNotes.deleteError'));
      }
    });
  }

  isDeleting(note: PortalNote): boolean {
    return this.deletingContextKey === this.contextKey(note);
  }

  sourceLabel(note: PortalNote): string {
    if (note.contextTitle) {
      return note.contextTitle;
    }

    const context = (note.contextType || 'Portal').replace(/([a-z])([A-Z])/g, '$1 $2');
    return note.contextId ? `${context} report` : `${context} page`;
  }

  preview(note: PortalNote): string {
    const content = (note.content || '').trim();
    if (!content) {
      return this.t('portalNotes.voiceOnly');
    }

    return content.length > 180 ? `${content.slice(0, 180).trim()}...` : content;
  }

  contextDetails(note: PortalNote): string[] {
    return note.contextDetails || [];
  }

  updatedLabel(note: PortalNote): string {
    const value = note.updatedAt || note.createdAt;
    return value ? new Date(value).toLocaleString() : '';
  }

  private contextKey(note: PortalNote): string {
    return `${note.contextType}:${note.contextId || ''}`;
  }

  private t(key: string): string {
    return this.transloco.translate(key);
  }
}


