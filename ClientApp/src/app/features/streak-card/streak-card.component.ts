import { CommonModule } from '@angular/common';
import { Component, effect, inject, signal } from '@angular/core';
import { AuthService } from '../../core/services/auth.service';
import { LearningSyncService } from '../../core/services/learning-sync.service';
import { ProgressService } from '../../core/services/progress.service';
import { UserProgress } from '../../shared/models/learning.models';

@Component({
  selector: 'app-streak-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './streak-card.component.html',
  styleUrl: './streak-card.component.scss',
})
export class StreakCardComponent {
  private readonly authService = inject(AuthService);
  private readonly progressService = inject(ProgressService);
  private readonly learningSyncService = inject(LearningSyncService);

  protected readonly progress = signal<UserProgress | null>(null);

  constructor() {
    effect(() => {
      this.authService.currentUser();
      this.learningSyncService.refreshTick();
      this.load();
    });
  }

  private load(): void {
    if (!this.authService.currentUser()) {
      this.progress.set(null);
      return;
    }

    this.progressService.getProgress().subscribe({
      next: (progress) => this.progress.set(progress),
      error: () => this.progress.set(null),
    });
  }
}
