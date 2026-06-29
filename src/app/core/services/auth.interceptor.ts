import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from './auth.service';

function withToken(req: HttpRequest<unknown>, token: string): HttpRequest<unknown> {
  return token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;
}

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const token = authService.token();
  const isAuthCall = req.url.includes('/auth/');

  return next(withToken(req, token)).pipe(
    catchError((error: unknown) => {
      const is401 = error instanceof HttpErrorResponse && error.status === 401;

      // Only attempt a refresh for authenticated, non-auth requests that 401.
      if (!is401 || isAuthCall || !token) {
        return throwError(() => error);
      }

      return authService.refresh().pipe(
        switchMap((response) => next(withToken(req, response.token))),
        catchError((refreshError: unknown) => {
          authService.logout();
          void router.navigateByUrl('/welcome');
          return throwError(() => refreshError);
        }),
      );
    }),
  );
};
