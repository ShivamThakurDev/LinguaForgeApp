import { CommonModule } from '@angular/common';
import { Component, effect, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { LearningSyncService } from '../../core/services/learning-sync.service';
import { RecommendationService } from '../../core/services/recommendation.service';
import { Recommendation } from '../../shared/models/learning.models';

@Component({
  selector: 'app-continue-lesson-card',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './continue-lesson-card.component.html',
  styleUrl: './continue-lesson-card.component.scss',
})
export class ContinueLessonCardComponent {
  private readonly authService = inject(AuthService);
  private readonly recommendationService = inject(RecommendationService);
  private readonly learningSyncService = inject(LearningSyncService);

  protected readonly recommendation = signal<Recommendation | null>(null);
  protected readonly isLoading = signal(false);

  constructor() {
    effect(() => {
      this.authService.currentUser();
      this.learningSyncService.refreshTick();
      this.load();
    });
  }

  protected get lessonRoute(): string[] {
    const key = this.recommendation()?.lessonKey;
    return key ? ['/lesson-player', key] : ['/learn', 'A1'];
  }

  private load(): void {
    if (!this.authService.currentUser()) {
      this.recommendation.set(null);
      return;
    }

    this.isLoading.set(true);
    this.recommendationService.getNext().subscribe({
      next: (recommendation) => this.recommendation.set(recommendation),
      error: () => this.recommendation.set(null),
      complete: () => this.isLoading.set(false),
    });
  }
}
