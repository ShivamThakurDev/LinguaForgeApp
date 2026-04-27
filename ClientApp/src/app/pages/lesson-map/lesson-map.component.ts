import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { LessonService } from '../../core/services/lesson.service';
import { LessonNodeComponent } from '../../features/lesson-node/lesson-node.component';
import { Lesson } from '../../shared/models/learning.models';

interface LessonMapNode extends Lesson {
  stepLabel: string;
  lane: 'left' | 'right';
  status: 'completed' | 'active' | 'locked';
  xpReward: number;
  durationMinutes: number;
}

@Component({
  selector: 'app-lesson-map',
  standalone: true,
  imports: [CommonModule, RouterLink, LessonNodeComponent],
  templateUrl: './lesson-map.component.html',
  styleUrl: './lesson-map.component.scss',
})
export class LessonMapComponent {
  private readonly activatedRoute = inject(ActivatedRoute);
  private readonly lessonService = inject(LessonService);

  protected readonly level = signal('A1');
  protected readonly lessons = signal<Lesson[]>([]);
  protected readonly isLoading = signal(true);
  protected readonly error = signal('');

  protected readonly mapNodes = computed<LessonMapNode[]>(() =>
    this.lessons().map((lesson, index) => ({
      ...lesson,
      stepLabel: `Lesson ${index + 1}`,
      lane: index % 2 === 0 ? 'left' : 'right',
      status: index === 0 ? 'completed' : index === 1 ? 'active' : 'locked',
      xpReward: 20 + index * 5,
      durationMinutes: 4 + (index % 3),
    })),
  );
  protected readonly completedCount = computed(
    () => this.mapNodes().filter((lesson) => lesson.status === 'completed').length,
  );
  protected readonly currentLesson = computed(
    () => this.mapNodes().find((lesson) => lesson.status === 'active') ?? null,
  );

  constructor() {
    this.activatedRoute.paramMap.subscribe((params) => {
      const level = (params.get('level') ?? 'A1').toUpperCase();
      this.level.set(level);
      this.loadLessons(level);
    });
  }

  private loadLessons(level: string): void {
    this.isLoading.set(true);
    this.error.set('');

    this.lessonService.getLessons(level).subscribe({
      next: (lessons) => this.lessons.set(lessons),
      error: (err: unknown) => this.error.set(`Failed to load ${level} lessons: ${String(err)}`),
      complete: () => this.isLoading.set(false),
    });
  }
}
