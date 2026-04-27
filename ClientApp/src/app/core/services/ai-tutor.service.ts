import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../enviornment/environment';

export interface ChatMessage {
  role: 'user' | 'assistant';
  content: string;
}

interface ChatResponse {
  message: string;
}

@Injectable({ providedIn: 'root' })
export class AiTutorService {
  private readonly baseUrl = `${environment.apiBaseUrl}/ai`;

  constructor(private readonly http: HttpClient) {}

  chat(conversationHistory: ChatMessage[]): Observable<ChatResponse> {
    return this.http.post<ChatResponse>(`${this.baseUrl}/chat`, { conversationHistory });
  }
}