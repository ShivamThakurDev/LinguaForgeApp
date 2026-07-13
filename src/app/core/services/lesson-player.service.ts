import { Injectable, inject } from '@angular/core';
import { Observable, forkJoin, map, tap } from 'rxjs';
import { Lesson } from '../../shared/models/learning.models';
import { LearningSyncService } from './learning-sync.service';
import { LessonService } from './lesson.service';
import { ProgressService } from './progress.service';

@Injectable({ providedIn: 'root' })
export class LessonPlayerService {
  private readonly lessonService = inject(LessonService);
  private readonly progressService = inject(ProgressService);
  private readonly learningSyncService = inject(LearningSyncService);
  private readonly supportedLevels = ['A1', 'A2', 'B1', 'B2'];

  getLessonByKey(lessonKey: string): Observable<Lesson | null> {
    return forkJoin(
      this.supportedLevels.map((level) =>
        this.lessonService.getLessons(level).pipe(
          map((lessons) => lessons.find((lesson) => lesson.lessonKey === lessonKey) ?? null),
        ),
      ),
    ).pipe(map((matches) => matches.find((entry) => entry !== null) ?? null));
  }

  completeLesson(lesson: Lesson): Observable<unknown> {
    // Server computes XP/accuracy from graded attempts; the client only names the lesson.
    return this.progressService
      .completeLesson({ lessonKey: lesson.lessonKey })
      .pipe(tap(() => this.learningSyncService.notifyUpdate()));
  }
}
