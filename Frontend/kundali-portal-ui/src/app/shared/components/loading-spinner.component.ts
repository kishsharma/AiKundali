// Loading Spinner Component - reusable loader
import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-loading-spinner',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="spinner-container" [class.full-screen]="fullScreen">
      <div class="spinner" [style.width]="size" [style.height]="size">
        <div class="spinner-ring"></div>
      </div>
      <p *ngIf="message" class="spinner-message">{{ message }}</p>
    </div>
  `,
  styles: [`
    .spinner-container {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      gap: 1rem;
      padding: 2rem;

      &.full-screen {
        position: fixed;
        top: 0;
        left: 0;
        width: 100%;
        height: 100%;
        background: rgba(255, 255, 255, 0.9);
        z-index: 9999;
        backdrop-filter: blur(2px);
      }
    }

    .spinner {
      display: inline-block;
      position: relative;
    }

    .spinner-ring {
      box-sizing: border-box;
      display: block;
      position: absolute;
      width: 100%;
      height: 100%;
      margin: 0;
      border: 4px solid #7E3BA8;
      border-radius: 50%;
      animation: spinner-ring 1.2s cubic-bezier(0.5, 0, 0.5, 1) infinite;
      border-color: #7E3BA8 transparent transparent transparent;
    }

    .spinner-ring:nth-child(1) {
      animation-delay: -0.45s;
    }
    .spinner-ring:nth-child(2) {
      animation-delay: -0.3s;
    }
    .spinner-ring:nth-child(3) {
      animation-delay: -0.15s;
    }

    @keyframes spinner-ring {
      0% {
        transform: rotate(0deg);
      }
      100% {
        transform: rotate(360deg);
      }
    }

    .spinner-message {
      color: #424242;
      font-size: 0.875rem;
      margin: 0;
      font-weight: 500;
    }
  `],
})
export class LoadingSpinnerComponent {
  @Input() size: string = '50px';
  @Input() message: string = '';
  @Input() fullScreen: boolean = false;
}
