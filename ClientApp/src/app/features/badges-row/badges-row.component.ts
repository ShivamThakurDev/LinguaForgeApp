import { CommonModule } from '@angular/common';
import { Component, effect, inject, signal } from '@angular/core';
import { AuthService } from '../../core/services/auth.service';
import { LearningSyncService } from '../../core/services/learning-sync.service';
import { ProgressService } from '../../core/services/progress.service';
import { ProgressBadge } from '../../shared/models/learning.models';

@Component({
  selector: 'app-badges-row',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './badges-row.component.html',
  styleUrl: './badges-row.component.scss',
})
export class BadgesRowComponent {
  private readonly authService = inject(AuthService);
  private readonly progressService = inject(ProgressService);
  private readonly learningSyncService = inject(LearningSyncService);

  protected readonly badges = signal<ProgressBadge[]>([]);

  constructor() {
    effect(() => {
      this.authService.currentUser();
      this.learningSyncService.refreshTick();
      this.load();
    });
  }

  private load(): void {
    if (!this.authService.currentUser()) {
      this.badges.set([]);
      return;
    }

    this.progressService.getProgress().subscribe({
      next: (progress) => this.badges.set(progress.badges),
      error: () => this.badges.set([]),
    });
  }
}
