import { CommonModule, isPlatformBrowser } from '@angular/common';
import { Component, OnInit, PLATFORM_ID, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LessonService } from '../../core/services/lesson.service';
import { Lesson } from '../../shared/models/learning.models';

@Component({
  selector: 'app-lesson',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './lesson.component.html',
  styleUrl: './lesson.component.scss',
})
export class LessonComponent implements OnInit {
  private readonly lessonService = inject(LessonService);
  private readonly platformId = inject(PLATFORM_ID);

  levels = ['A1', 'A2', 'B1', 'B2'];
  selectedLevel = signal<string>('A1');
  lessons = signal<Lesson[]>([]);
  selectedLessonKey = signal<string>('');
  isLoading = signal<boolean>(false);
  error = signal<string>('');

  selectedLesson = computed(() =>
    this.lessons().find((x) => x.lessonKey === this.selectedLessonKey()) ?? null,
  );

  ngOnInit(): void {
    this.loadLessons();
  }

  loadLessons(): void {
    this.isLoading.set(true);
    this.error.set('');

    this.lessonService.getLessons(this.selectedLevel()).subscribe({
      next: (data) => {
        this.lessons.set(data);
        this.selectedLessonKey.set(data[0]?.lessonKey ?? '');
      },
      error: (err: unknown) => {
        this.error.set(`Failed to load lessons: ${String(err)}`);
      },
      complete: () => this.isLoading.set(false),
    });
  }

  pickLesson(lessonKey: string): void {
    this.selectedLessonKey.set(lessonKey);
  }

  playWord(germanWord: string, audioUrl?: string | null): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }

    if (audioUrl) {
      const audio = new Audio(audioUrl);
      void audio.play();
      return;
    }

    const synth = window.speechSynthesis;
    const utterance = new SpeechSynthesisUtterance(germanWord);
    utterance.lang = 'de-DE';
    synth.cancel();
    synth.speak(utterance);
  }
}