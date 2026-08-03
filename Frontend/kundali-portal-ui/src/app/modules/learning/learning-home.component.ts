import { CommonModule } from '@angular/common';
import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { LearningLevelManifest, LearningLevelSummary, LearningSessionSummary } from './learning.models';
import { LearningService } from './learning.service';

@Component({
  selector: 'app-learning-home',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './learning-home.component.html',
  styleUrl: './learning-home.component.scss'
})
export class LearningHomeComponent implements OnInit {
  levels: LearningLevelSummary[] = [];
  level?: LearningLevelManifest;
  selectedLevelCode = 'level-1';
  loading = true;
  errorMessage = '';

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly learningService: LearningService,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadLevels();
    this.route.paramMap.subscribe(params => {
      this.loadLevel(params.get('levelCode') || 'level-1');
    });
  }

  selectLevel(level: LearningLevelSummary): void {
    if (!this.isLevelAvailable(level)) {
      return;
    }

    this.router.navigate(['/app/learning/level', level.code]);
  }

  openSession(session: LearningSessionSummary): void {
    if (!this.isSessionAvailable(session)) {
      return;
    }

    this.router.navigate(['/app/learning/session', session.id]);
  }

  isLevelAvailable(level: LearningLevelSummary): boolean {
    return level.status === 'published' || level.status === 'review' || level.status === 'content-in-progress' || level.code === 'level-1' || level.sessionCount > 0;
  }

  isSessionAvailable(session: LearningSessionSummary): boolean {
    return session.isPreviewAvailable === true || session.sessionNumber === 1 || session.status === 'published' || session.status === 'review' || session.status === 'content-in-progress';
  }

  private loadLevels(): void {
    this.learningService.getLevels().subscribe({
      next: levels => {
        this.levels = levels;
        this.cdr.markForCheck();
      },
      error: error => {
        console.error('Learning levels load error:', error);
      }
    });
  }

  private loadLevel(levelCode: string): void {
    this.selectedLevelCode = levelCode;
    this.loading = true;
    this.errorMessage = '';

    this.learningService.getLevel(levelCode).pipe(
      finalize(() => {
        this.loading = false;
        this.cdr.markForCheck();
      })
    ).subscribe({
      next: level => this.level = level,
      error: error => {
        console.error('Learning level load error:', error);
        this.errorMessage = 'Unable to load learning content right now.';
        this.cdr.markForCheck();
      }
    });
  }
}