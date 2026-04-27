import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../enviornment/environment';
import { Lesson } from '../../shared/models/learning.models';

@Injectable({ providedIn: 'root' })
export class LessonService {
  private readonly baseUrl = `${environment.apiBaseUrl}/lessons`;

  constructor(private readonly http: HttpClient) {}

  getLessons(level: string): Observable<Lesson[]> {
    return this.http.get<Lesson[]>(`${this.baseUrl}?level=${encodeURIComponent(level)}`);
  }
}