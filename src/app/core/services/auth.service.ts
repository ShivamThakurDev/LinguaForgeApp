import { Injectable, PLATFORM_ID, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { isPlatformBrowser } from '@angular/common';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../enviornment/environment';
import { AuthResponse, AuthUser } from '../../shared/models/learning.models';

interface AuthPayload {
  email: string;
  password: string;
  userName?: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly storageKey = 'linguaforge.auth';

  currentUser = signal<AuthUser | null>(null);
  token = signal<string>('');

  constructor() {
    this.restoreSession();
  }

  register(payload: AuthPayload): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${environment.apiBaseUrl}/auth/register`, payload).pipe(
      tap((response) => this.storeSession(response)),
    );
  }

  login(payload: AuthPayload): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${environment.apiBaseUrl}/auth/login`, payload).pipe(
      tap((response) => this.storeSession(response)),
    );
  }

  logout(): void {
    this.currentUser.set(null);
    this.token.set('');

    if (isPlatformBrowser(this.platformId)) {
      localStorage.removeItem(this.storageKey);
    }
  }

  private restoreSession(): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }

    const raw = localStorage.getItem(this.storageKey);
    if (!raw) {
      return;
    }

    try {
      const parsed = JSON.parse(raw) as AuthResponse;
      this.storeSession(parsed, false);
    } catch {
      localStorage.removeItem(this.storageKey);
    }
  }

  private storeSession(response: AuthResponse, persist = true): void {
    this.currentUser.set(response.user);
    this.token.set(response.token);

    if (persist && isPlatformBrowser(this.platformId)) {
      localStorage.setItem(this.storageKey, JSON.stringify(response));
    }
  }
}
