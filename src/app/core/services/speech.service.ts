import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../enviornment/environment';
import { PronunciationResult } from '../../shared/models/learning.models';

@Injectable({ providedIn: 'root' })
export class SpeechService {
  private readonly baseUrl = `${environment.apiBaseUrl}/speech`;

  constructor(private readonly http: HttpClient) {}

  assess(audio: Blob, referenceText: string, locale = 'de-DE'): Observable<PronunciationResult> {
    const formData = new FormData();
    formData.append('audio', audio, 'recording.wav');
    formData.append('referenceText', referenceText);
    formData.append('locale', locale);

    return this.http.post<PronunciationResult>(`${this.baseUrl}/assess`, formData);
  }
}