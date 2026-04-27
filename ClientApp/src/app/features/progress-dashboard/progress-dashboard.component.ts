import { CommonModule, isPlatformBrowser } from '@angular/common';
import { AfterViewInit, Component, ElementRef, PLATFORM_ID, ViewChild, effect, inject, signal } from '@angular/core';
import { AuthService } from '../../core/services/auth.service';
import { LearningSyncService } from '../../core/services/learning-sync.service';
import { ProgressService } from '../../core/services/progress.service';
import { UserProgress } from '../../shared/models/learning.models';

@Component({
  selector: 'app-progress-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './progress-dashboard.component.html',
  styleUrl: './progress-dashboard.component.scss',
})
export class ProgressDashboardComponent implements AfterViewInit {
  private readonly authService = inject(AuthService);
  private readonly learningSyncService = inject(LearningSyncService);
  private readonly progressService = inject(ProgressService);
  private readonly platformId = inject(PLATFORM_ID);

  @ViewChild('heatmapCanvas') heatmapCanvas?: ElementRef<HTMLCanvasElement>;

  progress = signal<UserProgress | null>(null);
  isLoading = signal<boolean>(false);
  error = signal<string>('');

  constructor() {
    effect(() => {
      this.progress();
      this.drawHeatmap();
    });

    effect(() => {
      this.authService.currentUser();
      this.learningSyncService.refreshTick();

      if (this.heatmapCanvas) {
        this.loadProgress();
      }
    });
  }

  ngAfterViewInit(): void {
    this.loadProgress();
  }

  loadProgress(): void {
    if (!this.authService.currentUser()) {
      this.progress.set(null);
      this.error.set('');
      return;
    }

    this.isLoading.set(true);
    this.error.set('');

    this.progressService.getProgress().subscribe({
      next: (res) => this.progress.set(res),
      error: (err: unknown) => this.error.set(`Could not load progress: ${String(err)}`),
      complete: () => this.isLoading.set(false),
    });
  }

  private drawHeatmap(): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }

    const canvas = this.heatmapCanvas?.nativeElement;
    const data = this.progress()?.heatmap ?? [];
    if (!canvas) {
      return;
    }

    const ctx = canvas.getContext('2d');
    if (!ctx) {
      return;
    }

    const cols = 15;
    const cell = 14;
    const gap = 4;
    canvas.width = cols * (cell + gap);
    canvas.height = 5 * (cell + gap);

    ctx.clearRect(0, 0, canvas.width, canvas.height);

    const values = data.map((x) => x.xp);
    const max = Math.max(...values, 1);

    for (let i = 0; i < cols * 5; i += 1) {
      const row = Math.floor(i / cols);
      const col = i % cols;
      const x = col * (cell + gap);
      const y = row * (cell + gap);

      const point = data[i];
      const intensity = point ? point.xp / max : 0;
      const shade = point ? `rgba(24, 184, 136, ${0.15 + intensity * 0.85})` : 'rgba(220,220,220,0.5)';

      ctx.fillStyle = shade;
      ctx.fillRect(x, y, cell, cell);
    }
  }
}
