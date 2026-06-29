import { CommonModule } from '@angular/common';
import { Component, effect, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { ProgressService } from '../../core/services/progress.service';
import { LearningSyncService } from '../../core/services/learning-sync.service';

interface LevelEntry {
  code: string;
  title: string;
  summary: string;
  accent: string;
}

interface PathPreviewNode {
  title: string;
  type: string;
  status: 'completed' | 'active' | 'locked';
  xp: number;
}

@Component({
  selector: 'app-learn-home',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './learn-home.component.html',
  styleUrl: './learn-home.component.scss',
})
export class LearnHomeComponent {
  private readonly authService = inject(AuthService);
  private readonly progressService = inject(ProgressService);
  private readonly learningSync = inject(LearningSyncService);

  // Real values for signed-in learners; sensible zero-state for guests.
  protected readonly streakDays = signal(0);
  protected readonly totalXp = signal(0);
  protected readonly dailyGoalCompleted = 2;
  protected readonly dailyGoalTotal = 5;
  protected readonly dailyGoalPercent = Math.round((this.dailyGoalCompleted / this.dailyGoalTotal) * 100);
  protected readonly activePath = 'A1 Starter Trail';
  protected readonly continueRoute = ['/learn', 'A1', 'a1-articles'];

  protected readonly pathPreview: PathPreviewNode[] = [
    { title: 'Basics warm-up', type: 'vocab sprint', status: 'completed', xp: 15 },
    { title: 'Articles in action', type: 'grammar mission', status: 'active', xp: 30 },
    { title: 'Daily routine phrases', type: 'speaking drill', status: 'locked', xp: 35 },
    { title: 'Question builder', type: 'pattern unlock', status: 'locked', xp: 40 },
  ];

  constructor() {
    // Pull live XP/streak for signed-in learners and refresh after each lesson.
    effect(() => {
      this.learningSync.refreshTick();

      if (!this.authService.currentUser()) {
        this.streakDays.set(0);
        this.totalXp.set(0);
        return;
      }

      this.progressService.getProgress().subscribe({
        next: (progress) => {
          this.streakDays.set(progress.currentStreakDays);
          this.totalXp.set(progress.totalXp);
        },
        error: () => {
          // Leave the zero-state on failure rather than blocking the page.
        },
      });
    });
  }

  protected readonly levels: LevelEntry[] = [
    {
      code: 'A1',
      title: 'Starter Trail',
      summary: 'Build first-need vocabulary, clear article usage, and confidence for everyday German.',
      accent: 'sunrise',
    },
    {
      code: 'A2',
      title: 'Momentum Loop',
      summary: 'Grow into routines, directions, and short conversations without losing structure.',
      accent: 'meadow',
    },
    {
      code: 'B1',
      title: 'Bridge Path',
      summary: 'Handle opinions, travel, and work scenarios with stronger grammar recall.',
      accent: 'ocean',
    },
    {
      code: 'B2',
      title: 'Fluency Ridge',
      summary: 'Tackle nuance, speed, and expressive German with deeper speaking challenges.',
      accent: 'ember',
    },
  ];
}
