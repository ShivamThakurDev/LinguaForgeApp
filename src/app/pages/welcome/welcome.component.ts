import { CommonModule, isPlatformBrowser } from '@angular/common';
import { Component, PLATFORM_ID, effect, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthPanelComponent } from '../../features/auth-panel/auth-panel.component';
import { AuthService } from '../../core/services/auth.service';

interface LearnGoal {
  code: string;
  emoji: string;
  title: string;
  blurb: string;
}

/**
 * First screen for new visitors: one clear promise, a low-commitment goal pick,
 * then signup/login. Once authenticated we send the learner straight to where
 * they were headed (returnUrl) or to the learn home.
 */
@Component({
  selector: 'app-welcome',
  standalone: true,
  imports: [CommonModule, RouterLink, AuthPanelComponent],
  templateUrl: './welcome.component.html',
  styleUrl: './welcome.component.scss',
})
export class WelcomeComponent {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly goalStorageKey = 'linguaforge.goal';

  protected readonly selectedGoal = signal<string>('');

  protected readonly goals: LearnGoal[] = [
    { code: 'travel', emoji: '✈️', title: 'Travel', blurb: 'Order, ask, and get around in German.' },
    { code: 'work', emoji: '💼', title: 'Work', blurb: 'Handle everyday workplace conversations.' },
    { code: 'curious', emoji: '🌱', title: 'Just curious', blurb: 'Learn a little every day for fun.' },
  ];

  constructor() {
    // The moment auth succeeds (via the embedded auth panel), move the learner on.
    effect(() => {
      if (this.authService.currentUser()) {
        const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') || '/learn';
        void this.router.navigateByUrl(returnUrl);
      }
    });
  }

  protected pickGoal(code: string): void {
    this.selectedGoal.set(code);
    if (isPlatformBrowser(this.platformId)) {
      localStorage.setItem(this.goalStorageKey, code);
    }
  }
}
