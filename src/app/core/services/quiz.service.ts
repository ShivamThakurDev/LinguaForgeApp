import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../enviornment/environment';
import { GenerateExerciseResponse, QuizEvaluationRequest, QuizEvaluationResponse } from '../../shared/models/learning.models';

export interface GenerateQuizPayload {
  topic: string;
  level: string;
  exerciseType: string;
}

@Injectable({ providedIn: 'root' })
export class QuizService {
  private readonly baseUrl = `${environment.apiBaseUrl}/quiz`;

  constructor(private readonly http: HttpClient) {}

  generate(payload: GenerateQuizPayload): Observable<GenerateExerciseResponse> {
    return this.http.post<GenerateExerciseResponse>(`${this.baseUrl}/generate`, payload);
  }

  evaluate(payload: QuizEvaluationRequest): Observable<QuizEvaluationResponse> {
    return this.http.post<QuizEvaluationResponse>(`${this.baseUrl}/evaluate`, payload);
  }
}
