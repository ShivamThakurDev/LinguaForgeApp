import { CommonModule } from '@angular/common';
import { Component, effect, inject, signal } from '@angular/core';
import { AuthService } from '../../core/services/auth.service';
import { LearningSyncService } from '../../core/services/learning-sync.service';
import { RecommendationService } from '../../core/services/recommendation.service';
import { Recommendation } from '../../shared/models/learning.models';

@Component({
  selector: 'app-weak-topics-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './weak-topics-card.component.html',
  styleUrl: './weak-topics-card.component.scss',
})
export class WeakTopicsCardComponent {
  private readonly authService = inject(AuthService);
  private readonly recommendationService = inject(RecommendationService);
  private readonly learningSyncService = inject(LearningSyncService);

  protected readonly recommendation = signal<Recommendation | null>(null);

  constructor() {
    effect(() => {
      this.authService.currentUser();
      this.learningSyncService.refreshTick();
      this.load();
    });
  }

  private load(): void {
    if (!this.authService.currentUser()) {
      this.recommendation.set(null);
      return;
    }

    this.recommendationService.getNext().subscribe({
      next: (recommendation) => this.recommendation.set(recommendation),
      error: () => this.recommendation.set(null),
    });
  }
}
