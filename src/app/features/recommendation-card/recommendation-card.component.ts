import { CommonModule } from '@angular/common';
import { Component, effect, inject, signal } from '@angular/core';
import { AuthService } from '../../core/services/auth.service';
import { LearningSyncService } from '../../core/services/learning-sync.service';
import { RecommendationService } from '../../core/services/recommendation.service';
import { Recommendation } from '../../shared/models/learning.models';

@Component({
  selector: 'app-recommendation-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './recommendation-card.component.html',
  styleUrl: './recommendation-card.component.scss',
})
export class RecommendationCardComponent {
  private readonly authService = inject(AuthService);
  private readonly recommendationService = inject(RecommendationService);
  private readonly learningSyncService = inject(LearningSyncService);

  recommendation = signal<Recommendation | null>(null);
  isLoading = signal(false);
  error = signal('');

  constructor() {
    effect(() => {
      this.authService.currentUser();
      this.learningSyncService.refreshTick();
      this.load();
    });
  }

  load(): void {
    if (!this.authService.currentUser()) {
      this.recommendation.set(null);
      this.error.set('');
      return;
    }

    this.isLoading.set(true);
    this.error.set('');

    this.recommendationService.getNext().subscribe({
      next: (result) => this.recommendation.set(result),
      error: (err: unknown) => this.error.set(`Could not load recommendation: ${String(err)}`),
      complete: () => this.isLoading.set(false),
    });
  }
}
