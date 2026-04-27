import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-progress-page',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './progress-page.component.html',
  styleUrl: './progress-page.component.scss',
})
export class ProgressPageComponent {
  protected readonly totalXp = 1840;
  protected readonly weeklyXp = 240;
  protected readonly streakDays = 12;
  protected readonly lessonsCompleted = 28;
  protected readonly nextMilestone = 2000;
  protected readonly badges = [
    { icon: '🔥', name: 'Hot week', note: '7-day streak held' },
    { icon: '🗣', name: 'Voice ready', note: '5 speaking tasks done' },
    { icon: '🧠', name: 'Grammar spark', note: 'Article lesson streak' },
  ];
}
