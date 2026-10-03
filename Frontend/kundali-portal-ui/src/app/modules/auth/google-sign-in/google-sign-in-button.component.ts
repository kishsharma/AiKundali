import { CommonModule } from '@angular/common';
import { AfterViewInit, Component, ElementRef, EventEmitter, Input, Output, ViewChild } from '@angular/core';
import { environment } from '../../../config/environment/environment';

type GoogleButtonContext = 'signin' | 'signup';

interface GoogleCredentialResponse {
  credential?: string;
}

interface GoogleIdApi {
  initialize(config: {
    client_id: string;
    callback: (response: GoogleCredentialResponse) => void;
    auto_select?: boolean;
    cancel_on_tap_outside?: boolean;
  }): void;
  renderButton(parent: HTMLElement, options: Record<string, string | number>): void;
}

declare global {
  interface Window {
    google?: {
      accounts?: {
        id?: GoogleIdApi;
      };
    };
  }
}

let googleIdentityScript: Promise<void> | null = null;

function loadGoogleIdentityScript(): Promise<void> {
  if (window.google?.accounts?.id) {
    return Promise.resolve();
  }

  if (googleIdentityScript) {
    return googleIdentityScript;
  }

  googleIdentityScript = new Promise((resolve, reject) => {
    const script = document.createElement('script');
    script.src = 'https://accounts.google.com/gsi/client';
    script.async = true;
    script.defer = true;
    script.onload = () => resolve();
    script.onerror = () => {
      googleIdentityScript = null;
      reject(new Error('Google sign-in failed to load.'));
    };
    document.head.appendChild(script);
  });

  return googleIdentityScript;
}

@Component({
  selector: 'app-google-sign-in-button',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './google-sign-in-button.component.html',
  styleUrl: './google-sign-in-button.component.scss'
})
export class GoogleSignInButtonComponent implements AfterViewInit {
  @Input() context: GoogleButtonContext = 'signin';
  @Input() disabled = false;
  @Output() credential = new EventEmitter<string>();
  @Output() failed = new EventEmitter<string>();

  @ViewChild('buttonHost') buttonHost?: ElementRef<HTMLElement>;

  readonly clientId = environment.googleClientId?.trim() ?? '';

  ngAfterViewInit(): void {
    if (!this.clientId) {
      return;
    }

    void this.renderButton();
  }

  private async renderButton(): Promise<void> {
    try {
      await loadGoogleIdentityScript();
      const googleId = window.google?.accounts?.id;
      const host = this.buttonHost?.nativeElement;
      if (!googleId || !host) {
        this.failed.emit('Google sign-in failed to load.');
        return;
      }

      googleId.initialize({
        client_id: this.clientId,
        callback: (response) => {
          if (response.credential) {
            this.credential.emit(response.credential);
          }
        },
        auto_select: false,
        cancel_on_tap_outside: true
      });

      const width = Math.min(400, Math.max(240, Math.floor(host.clientWidth || 366)));
      googleId.renderButton(host, {
        type: 'standard',
        theme: 'outline',
        size: 'large',
        text: this.context === 'signup' ? 'signup_with' : 'signin_with',
        shape: 'rectangular',
        logo_alignment: 'left',
        width
      });
    } catch {
      this.failed.emit('Google sign-in failed to load.');
    }
  }
}
