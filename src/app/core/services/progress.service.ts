import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../enviornment/environment';
import { UserProgress } from '../../shared/models/learning.models';

export interface CompleteLessonPayload {
  // The client only names the lesson; XP/accuracy/title are computed server-side.
  lessonKey: string;
}

@Injectable({ providedIn: 'root' })
export class ProgressService {
  private readonly baseUrl = `${environment.apiBaseUrl}/user`;

  constructor(private readonly http: HttpClient) {}

  getProgress(): Observable<UserProgress> {
    return this.http.get<UserProgress>(`${this.baseUrl}/progress`);
  }

  completeLesson(payload: CompleteLessonPayload): Observable<UserProgress> {
    return this.http.post<UserProgress>(`${this.baseUrl}/progress/lesson-complete`, payload);
  }
}
