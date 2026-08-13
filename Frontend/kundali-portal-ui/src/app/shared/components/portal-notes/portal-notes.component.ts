import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, OnChanges, OnDestroy, Output, SimpleChanges, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PortalNoteVoiceRecording, PortalNotesService } from '../../../core/services/portal-notes.service';
import { NotificationService } from '../../services/notification.service';

interface PendingVoiceRecording {
  id: string;
  blob: Blob;
  fileName: string;
  contentType: string;
  durationSeconds: number;
  audioUrl: string;
}

interface VoiceRecordingView extends PortalNoteVoiceRecording {
  audioUrl?: string;
  loadingAudio?: boolean;
}

@Component({
  selector: 'app-portal-notes',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './portal-notes.component.html',
  styleUrl: './portal-notes.component.scss'
})
export class PortalNotesComponent implements OnChanges, OnDestroy {
  @Input({ required: true }) contextType = '';
  @Input() contextId: string | null = null;
  @Input() title = 'Notes';
  @Input() subtitle = 'Keep private notes for this record.';
  @Input() pendingMessage = 'You can write notes now. They will be saved when this record is saved.';
  @Input() open = false;
  @Input() allowPageSave = false;
  @Output() closed = new EventEmitter<void>();

  content = '';
  savedContent = '';
  contextTitle = '';
  contextDetails: string[] = [];
  loading = false;
  saving = false;
  deleting = false;
  loadedContextKey = '';
  voiceRecordings: VoiceRecordingView[] = [];
  pendingVoiceRecordings: PendingVoiceRecording[] = [];
  loadingVoice = false;
  uploadingVoice = false;
  recording = false;
  recordingSeconds = 0;
  maxVoiceDurationSeconds = 120;
  mediaRecorderSupported = typeof MediaRecorder !== 'undefined';

  private mediaRecorder?: MediaRecorder;
  private recordingChunks: BlobPart[] = [];
  private recordingStartedAt = 0;
  private recordingTimer?: ReturnType<typeof setInterval>;
  private stopRecordingTimer?: ReturnType<typeof setTimeout>;
  private readonly objectUrls: string[] = [];
  private readonly notesService = inject(PortalNotesService);
  private readonly notifications = inject(NotificationService);

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['contextId'] || changes['contextType'] || changes['allowPageSave']) {
      this.loadCurrentNote();
      this.loadVoiceRecordings();
    }
  }

  ngOnDestroy(): void {
    this.stopRecordingTracks();
    this.clearRecordingTimers();
    this.objectUrls.forEach((url) => URL.revokeObjectURL(url));
  }

  get canPersistNotes(): boolean {
    return !!this.contextType && (!!this.contextId || this.allowPageSave);
  }

  get hasChanges(): boolean {
    return this.content.trim() !== this.savedContent.trim();
  }

  get hasSavedNote(): boolean {
    return !!this.savedContent.trim();
  }

  get hasPendingDraft(): boolean {
    return this.hasChanges || this.pendingVoiceRecordings.length > 0;
  }

  get formattedRecordingTime(): string {
    return this.formatDuration(this.recordingSeconds);
  }

  close(): void {
    this.closed.emit();
  }

  save(): void {
    if (!this.canPersistNotes || this.saving) {
      return;
    }

    this.saveForContext(this.contextId);
  }

  saveForContext(contextId?: string | null): void {
    if (!this.contextType || (!contextId && !this.allowPageSave) || this.saving || this.uploadingVoice || !this.hasPendingDraft) {
      return;
    }

    this.contextId = contextId || null;
    this.loadedContextKey = `${this.contextType}:${contextId || ''}`;

    if (this.hasChanges) {
      this.saving = true;
      this.notesService.saveNote({
        contextType: this.contextType,
        contextId: contextId || null,
        content: this.content
      }).subscribe({
        next: (note) => {
          this.content = note.content || '';
          this.savedContent = this.content;
          this.contextTitle = note.contextTitle || '';
          this.contextDetails = note.contextDetails || [];
          this.saving = false;
          this.uploadPendingVoiceRecordings(contextId || null);
          this.notifications.success('Notes saved.');
        },
        error: () => {
          this.saving = false;
          this.notifications.error('Unable to save notes.');
        }
      });
      return;
    }

    this.uploadPendingVoiceRecordings(contextId || null);
  }

  clear(): void {
    if (!this.canPersistNotes || this.deleting || !this.hasSavedNote) {
      this.content = '';
      this.clearPendingVoiceRecordings();
      return;
    }

    this.deleting = true;
    this.notesService.deleteNote(this.contextType, this.contextId || null).subscribe({
      next: () => {
        this.content = '';
        this.savedContent = '';
        this.contextTitle = '';
        this.contextDetails = [];
        this.deleting = false;
        this.notifications.success('Notes cleared.');
      },
      error: () => {
        this.deleting = false;
        this.notifications.error('Unable to clear notes.');
      }
    });
  }

  startVoiceRecording(): void {
    if (!this.mediaRecorderSupported || this.recording) {
      return;
    }

    navigator.mediaDevices?.getUserMedia({ audio: true }).then((stream) => {
      this.recordingChunks = [];
      this.mediaRecorder = new MediaRecorder(stream);
      this.mediaRecorder.ondataavailable = (event) => {
        if (event.data.size > 0) {
          this.recordingChunks.push(event.data);
        }
      };
      this.mediaRecorder.onstop = () => this.captureStoppedRecording();
      this.mediaRecorder.start();
      this.recording = true;
      this.recordingSeconds = 0;
      this.recordingStartedAt = Date.now();
      this.recordingTimer = setInterval(() => {
        this.recordingSeconds = Math.min(
          this.maxVoiceDurationSeconds,
          Math.floor((Date.now() - this.recordingStartedAt) / 1000)
        );
      }, 500);
      this.stopRecordingTimer = setTimeout(() => this.stopVoiceRecording(), this.maxVoiceDurationSeconds * 1000);
    }).catch(() => {
      this.notifications.error('Microphone permission is required to record voice notes.');
    });
  }

  stopVoiceRecording(): void {
    if (!this.mediaRecorder || this.mediaRecorder.state === 'inactive') {
      return;
    }

    this.mediaRecorder.stop();
  }

  removePendingVoiceRecording(id: string): void {
    const pending = this.pendingVoiceRecordings.find((item) => item.id === id);
    if (pending) {
      URL.revokeObjectURL(pending.audioUrl);
    }

    this.pendingVoiceRecordings = this.pendingVoiceRecordings.filter((item) => item.id !== id);
  }

  loadVoiceAudio(recording: VoiceRecordingView): void {
    if (recording.audioUrl || recording.loadingAudio) {
      return;
    }

    recording.loadingAudio = true;
    this.notesService.downloadVoiceRecording(recording.id).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        this.objectUrls.push(url);
        recording.audioUrl = url;
        recording.loadingAudio = false;
      },
      error: () => {
        recording.loadingAudio = false;
        this.notifications.error('Unable to load voice recording.');
      }
    });
  }

  deleteVoiceRecording(recording: VoiceRecordingView): void {
    this.notesService.deleteVoiceRecording(recording.id).subscribe({
      next: () => {
        if (recording.audioUrl) {
          URL.revokeObjectURL(recording.audioUrl);
        }
        this.voiceRecordings = this.voiceRecordings.filter((item) => item.id !== recording.id);
        this.notifications.success('Voice recording deleted.');
      },
      error: () => this.notifications.error('Unable to delete voice recording.')
    });
  }

  formatDuration(totalSeconds?: number | null): string {
    const seconds = Math.max(0, Math.round(totalSeconds || 0));
    const minutes = Math.floor(seconds / 60);
    const remainingSeconds = seconds % 60;
    return `${minutes}:${remainingSeconds.toString().padStart(2, '0')}`;
  }

  private loadCurrentNote(): void {
    const contextKey = `${this.contextType}:${this.contextId || ''}`;
    if (!this.canPersistNotes) {
      const hadOnlySavedContent = this.content.trim() === this.savedContent.trim();
      this.loadedContextKey = contextKey;
      this.savedContent = '';
      this.contextTitle = '';
      this.contextDetails = [];
      if (hadOnlySavedContent) {
        this.content = '';
      }
      this.loading = false;
      return;
    }

    if (this.loadedContextKey === contextKey) {
      return;
    }

    this.loadedContextKey = contextKey;
    this.loading = true;
    this.content = '';
    this.savedContent = '';

    this.notesService.getNote(this.contextType, this.contextId || null).subscribe({
      next: (note) => {
        this.content = note.content || '';
        this.savedContent = this.content;
        this.contextTitle = note.contextTitle || '';
        this.contextDetails = note.contextDetails || [];
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.notifications.error('Unable to load notes.');
      }
    });
  }

  private loadVoiceRecordings(): void {
    this.notesService.getVoiceOptions().subscribe({
      next: (options) => {
        this.maxVoiceDurationSeconds = options.maxDurationSeconds || 120;
      }
    });

    if (!this.canPersistNotes) {
      this.voiceRecordings = [];
      return;
    }

    this.loadingVoice = true;
    this.notesService.getVoiceRecordings(this.contextType, this.contextId || null).subscribe({
      next: (recordings) => {
        this.voiceRecordings = recordings;
        this.loadingVoice = false;
      },
      error: () => {
        this.loadingVoice = false;
        this.notifications.error('Unable to load voice recordings.');
      }
    });
  }

  private uploadPendingVoiceRecordings(contextId?: string | null): void {
    if (!this.pendingVoiceRecordings.length || !this.contextType || (!contextId && !this.allowPageSave)) {
      return;
    }

    const pending = [...this.pendingVoiceRecordings];
    this.uploadingVoice = true;
    let completed = 0;
    let failed = false;

    pending.forEach((recording) => {
      this.notesService.uploadVoiceRecording(
        this.contextType,
        contextId || null,
        recording.blob,
        recording.durationSeconds,
        recording.fileName
      ).subscribe({
        next: (savedRecording) => {
          this.voiceRecordings = [savedRecording, ...this.voiceRecordings];
          this.removePendingVoiceRecording(recording.id);
          completed += 1;
          this.finishPendingVoiceUploadIfDone(completed, pending.length, failed);
        },
        error: () => {
          failed = true;
          completed += 1;
          this.finishPendingVoiceUploadIfDone(completed, pending.length, failed);
        }
      });
    });
  }

  private finishPendingVoiceUploadIfDone(completed: number, total: number, failed: boolean): void {
    if (completed < total) {
      return;
    }

    this.uploadingVoice = false;
    if (failed) {
      this.notifications.error('Some voice recordings could not be saved.');
      return;
    }

    this.notifications.success('Voice recordings saved.');
  }

  private captureStoppedRecording(): void {
    const durationSeconds = Math.min(
      this.maxVoiceDurationSeconds,
      Math.max(1, Math.ceil((Date.now() - this.recordingStartedAt) / 1000))
    );
    const contentType = this.mediaRecorder?.mimeType || 'audio/webm';
    const blob = new Blob(this.recordingChunks, { type: contentType });
    const audioUrl = URL.createObjectURL(blob);

    this.pendingVoiceRecordings = [
      ...this.pendingVoiceRecordings,
      {
        id: `${Date.now()}-${Math.random().toString(36).slice(2)}`,
        blob,
        fileName: `voice-note-${new Date().toISOString().replace(/[:.]/g, '-')}.webm`,
        contentType,
        durationSeconds,
        audioUrl
      }
    ];

    this.recording = false;
    this.recordingSeconds = durationSeconds;
    this.stopRecordingTracks();
    this.clearRecordingTimers();
  }

  private clearPendingVoiceRecordings(): void {
    this.pendingVoiceRecordings.forEach((item) => URL.revokeObjectURL(item.audioUrl));
    this.pendingVoiceRecordings = [];
  }

  private stopRecordingTracks(): void {
    this.mediaRecorder?.stream.getTracks().forEach((track) => track.stop());
    this.mediaRecorder = undefined;
  }

  private clearRecordingTimers(): void {
    if (this.recordingTimer) {
      clearInterval(this.recordingTimer);
      this.recordingTimer = undefined;
    }

    if (this.stopRecordingTimer) {
      clearTimeout(this.stopRecordingTimer);
      this.stopRecordingTimer = undefined;
    }
  }
}

