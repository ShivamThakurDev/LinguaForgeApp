import { CommonModule } from '@angular/common';
import { Component, computed, effect, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ProgressService } from '../../core/services/progress.service';
import { LearningSyncService } from '../../core/services/learning-sync.service';
import { UserProgress } from '../../shared/models/learning.models';

@Component({
  selector: 'app-progress-page',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './progress-page.component.html',
  styleUrl: './progress-page.component.scss',
})
export class ProgressPageComponent {
  private readonly progressService = inject(ProgressService);
  private readonly learningSync = inject(LearningSyncService);

  protected readonly isLoading = signal(true);
  protected readonly error = signal('');
  protected readonly progress = signal<UserProgress | null>(null);

  protected readonly totalXp = computed(() => this.progress()?.totalXp ?? 0);
  protected readonly streakDays = computed(() => this.progress()?.currentStreakDays ?? 0);
  protected readonly level = computed(() => this.progress()?.level ?? 1);
  protected readonly badges = computed(() => this.progress()?.badges ?? []);

  // XP earned in the last 7 days, summed from the activity heatmap.
  protected readonly weeklyXp = computed(() => {
    const points = this.progress()?.heatmap ?? [];
    return points.slice(-7).reduce((sum, point) => sum + point.xp, 0);
  });

  // Next reward sits at the next 500-XP boundary above the current total.
  protected readonly nextMilestone = computed(() => {
    const total = this.totalXp();
    return Math.max(500, (Math.floor(total / 500) + 1) * 500);
  });

  protected readonly milestonePercent = computed(() =>
    Math.min(100, Math.round((this.totalXp() / this.nextMilestone()) * 100)),
  );

  constructor() {
    // Reload progress whenever a lesson is completed elsewhere in the app.
    effect(() => {
      this.learningSync.refreshTick();
      this.load();
    });
  }

  private load(): void {
    this.isLoading.set(true);
    this.error.set('');

    this.progressService.getProgress().subscribe({
      next: (progress) => this.progress.set(progress),
      error: () =>
        this.error.set('We could not load your progress. Please try again in a moment.'),
      complete: () => this.isLoading.set(false),
    });
  }
}
