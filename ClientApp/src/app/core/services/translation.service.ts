import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { LanguageOption, TranslateRequestDto, TranslateResponseDto } from '../../shared/models/translation.models';
import { environment } from '../../../enviornment/environment';

interface LanguagesResponse extends Record<string, string> {}

@Injectable({
  providedIn: 'root',
})
export class TranslationService {
private readonly baseUrl = `${environment.apiBaseUrl}/translation`;
  constructor(private http: HttpClient) {}

  translate(request: TranslateRequestDto):Observable<TranslateResponseDto> {
    return this.http.post<TranslateResponseDto>(`${this.baseUrl}/translate`, request);
  }

  getSupportedLanguages(): Observable<LanguageOption[]> {
    return this.http.get<Record<string,string>>(`${this.baseUrl}/languages`).pipe(
      map((response) => {
        return Object.entries(response).map(([code, name]) =>
           ({ code, name }))
            .sort((a,b)=> 
              a.name.localeCompare(b.name));
      })
    );
}
}