import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../core/services/auth.service';
import { LearningSyncService } from '../../core/services/learning-sync.service';

@Component({
  selector: 'app-auth-panel',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './auth-panel.component.html',
  styleUrl: './auth-panel.component.scss',
})
export class AuthPanelComponent {
  private readonly authService = inject(AuthService);
  private readonly learningSyncService = inject(LearningSyncService);

  mode = signal<'login' | 'register'>('register');
  userName = signal('Learner');
  email = signal('');
  password = signal('');
  isLoading = signal(false);
  error = signal('');

  currentUser = this.authService.currentUser;
  isAuthenticated = computed(() => !!this.currentUser());

  submit(): void {
    this.error.set('');
    this.isLoading.set(true);

    const request$ = this.mode() === 'register'
      ? this.authService.register({
          userName: this.userName(),
          email: this.email(),
          password: this.password(),
        })
      : this.authService.login({
          email: this.email(),
          password: this.password(),
        });

    request$.subscribe({
      next: () => {
        this.password.set('');
        this.learningSyncService.notifyUpdate();
      },
      error: (err: { error?: { error?: string } }) => {
        this.error.set(err.error?.error ?? 'Authentication failed.');
      },
      complete: () => this.isLoading.set(false),
    });
  }

  logout(): void {
    this.authService.logout();
    this.learningSyncService.notifyUpdate();
  }
}
