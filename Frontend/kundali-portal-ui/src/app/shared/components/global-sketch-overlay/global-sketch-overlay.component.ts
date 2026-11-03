import { ChangeDetectorRef, Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  NgWhiteboardComponent,
  NgWhiteboardService,
  FormatType,
  ToolType,
  WhiteboardConfig,
  WhiteboardElement
} from 'ng-whiteboard';

@Component({
  selector: 'app-global-sketch-overlay',
  standalone: true,
  imports: [CommonModule, NgWhiteboardComponent],
  providers: [NgWhiteboardService],
  templateUrl: './global-sketch-overlay.component.html',
  styleUrl: './global-sketch-overlay.component.scss'
})
export class GlobalSketchOverlayComponent {
  private readonly whiteboard = inject(NgWhiteboardService);
  private readonly changeDetector = inject(ChangeDetectorRef);

  readonly boardId = 'horalife-global-sketch-board';
  readonly toolType = ToolType;
  readonly colorPresets = ['#C56F50', '#10143d', '#ffffff', '#1b7f4e', '#c62828', '#1565c0'];
  selectedTool: ToolType = ToolType.Pen;
  strokeColor = '#C56F50';
  strokeWidth = 3;
  boardReady = false;
  hasStrokes = false;
  zoomPercent = 100;

  whiteboardConfig: Partial<WhiteboardConfig> = {
    backgroundColor: 'transparent',
    strokeColor: this.strokeColor,
    strokeWidth: this.strokeWidth,
    fill: 'transparent',
    fullScreen: true,
    center: true,
    drawingEnabled: true,
    enableGrid: false,
    selectAfterDraw: false
  };

  onBoardReady(): void {
    this.boardReady = true;
    this.whiteboard.setActiveBoard(this.boardId);
  }

  onDataChange(elements: WhiteboardElement[]): void {
    this.hasStrokes = Array.isArray(elements) && elements.length > 0;
  }

  setTool(tool: ToolType): void {
    this.selectedTool = tool;
    if (this.boardReady) {
      this.whiteboard.setActiveBoard(this.boardId);
      this.whiteboard.setActiveTool(tool);
    }
  }

  setStrokeColor(color: string): void {
    this.strokeColor = color;
    this.whiteboardConfig = { ...this.whiteboardConfig, strokeColor: color };
  }

  setStrokeWidth(value: number | string): void {
    const width = Number(value);
    if (!Number.isFinite(width) || width < 1 || width > 12) {
      return;
    }
    this.strokeWidth = width;
    this.whiteboardConfig = { ...this.whiteboardConfig, strokeWidth: width };
  }

  undo(): void {
    if (!this.boardReady) return;
    this.whiteboard.setActiveBoard(this.boardId);
    this.whiteboard.undo();
  }

  redo(): void {
    if (!this.boardReady) return;
    this.whiteboard.setActiveBoard(this.boardId);
    this.whiteboard.redo();
  }

  clearBoard(): void {
    if (!this.boardReady) return;
    this.whiteboard.setActiveBoard(this.boardId);
    this.whiteboard.clear();
    this.hasStrokes = false;
    this.changeDetector.markForCheck();
  }

  zoomIn(): void {
    if (this.boardReady) {
      this.whiteboard.setActiveBoard(this.boardId);
      this.whiteboard.zoomIn();
    }
    this.zoomPercent = Math.min(125, this.zoomPercent + 10);
  }

  zoomOut(): void {
    if (this.boardReady) {
      this.whiteboard.setActiveBoard(this.boardId);
      this.whiteboard.zoomOut();
    }
    this.zoomPercent = Math.max(50, this.zoomPercent - 10);
  }

  resetZoom(): void {
    if (this.boardReady) {
      this.whiteboard.setActiveBoard(this.boardId);
      this.whiteboard.resetZoom();
    }
    this.zoomPercent = 100;
  }

  async downloadPng(): Promise<void> {
    if (!this.boardReady || !this.hasStrokes) return;
    this.whiteboard.setActiveBoard(this.boardId);
    await this.whiteboard.save(FormatType.Png, 'horalife-sketch');
  }

  ngOnDestroy(): void {
    try {
      this.whiteboard.setActiveBoard(null);
    } catch {
      // The board may already have been destroyed by Angular.
    }
  }
}
