import { Injectable, PLATFORM_ID, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { isPlatformBrowser } from '@angular/common';
import { Observable, finalize, shareReplay, tap } from 'rxjs';
import { environment } from '../../../enviornment/environment';
import { AuthResponse, AuthUser } from '../../shared/models/learning.models';

interface AuthPayload {
  email: string;
  password: string;
  userName?: string;
}

/**
 * Holds only the short-lived access JWT (in memory) and basic user info. The long-lived
 * refresh token lives in an HttpOnly cookie the browser manages for us — it is never read,
 * stored, or sent by JavaScript, so an XSS cannot exfiltrate it. Durable sessions come from
 * a silent cookie-based refresh on startup, not from localStorage. (LF-104)
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly platformId = inject(PLATFORM_ID);

  // Legacy key from the pre-cookie build; cleared on startup so stale refresh tokens don't linger.
  private readonly legacyStorageKey = 'linguaforge.auth';

  currentUser = signal<AuthUser | null>(null);
  token = signal<string>('');

  // Shared so concurrent 401s trigger a single refresh, not a stampede.
  private refreshInFlight?: Observable<AuthResponse>;

  constructor() {
    if (isPlatformBrowser(this.platformId)) {
      localStorage.removeItem(this.legacyStorageKey);
    }
  }

  /**
   * Restores the session on app start via the HttpOnly refresh cookie (browser only).
   * Resolves regardless of outcome so bootstrap is never blocked: no cookie → stays anonymous.
   */
  initialize(): Promise<void> {
    if (!isPlatformBrowser(this.platformId)) {
      return Promise.resolve();
    }

    return new Promise((resolve) => {
      this.refresh().subscribe({ next: () => resolve(), error: () => resolve() });
    });
  }

  register(payload: AuthPayload): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${environment.apiBaseUrl}/auth/register`, payload, { withCredentials: true })
      .pipe(tap((response) => this.storeSession(response)));
  }

  login(payload: AuthPayload): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${environment.apiBaseUrl}/auth/login`, payload, { withCredentials: true })
      .pipe(tap((response) => this.storeSession(response)));
  }

  /**
   * Exchanges the refresh cookie for a new access token (single-flight). No token travels in
   * the request body — `withCredentials` lets the browser attach the HttpOnly cookie instead.
   */
  refresh(): Observable<AuthResponse> {
    if (this.refreshInFlight) {
      return this.refreshInFlight;
    }

    this.refreshInFlight = this.http
      .post<AuthResponse>(`${environment.apiBaseUrl}/auth/refresh`, {}, { withCredentials: true })
      .pipe(
        tap((response) => this.storeSession(response)),
        finalize(() => (this.refreshInFlight = undefined)),
        shareReplay(1),
      );

    return this.refreshInFlight;
  }

  logout(): void {
    // Best-effort server-side revocation + cookie clear; ignore the outcome.
    this.http
      .post(`${environment.apiBaseUrl}/auth/logout`, {}, { withCredentials: true })
      .subscribe({ next: () => {}, error: () => {} });

    this.currentUser.set(null);
    this.token.set('');
  }

  private storeSession(response: AuthResponse): void {
    this.currentUser.set(response.user);
    this.token.set(response.token);
  }
}
