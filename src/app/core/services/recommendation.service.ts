import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../enviornment/environment';
import { Recommendation } from '../../shared/models/learning.models';

@Injectable({ providedIn: 'root' })
export class RecommendationService {
  private readonly baseUrl = `${environment.apiBaseUrl}/recommendations`;

  constructor(private readonly http: HttpClient) {}

  getNext(): Observable<Recommendation> {
    return this.http.get<Recommendation>(`${this.baseUrl}/next`);
  }
}
