import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, inject, provideAppInitializer, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideClientHydration, withEventReplay } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { authInterceptor } from './core/services/auth.interceptor';
import { AuthService } from './core/services/auth.service';
import { appRoutes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(appRoutes),
    provideHttpClient(withFetch(), withInterceptors([authInterceptor])),
    // Restore the session from the HttpOnly refresh cookie before the first route resolves,
    // so a page reload keeps the user signed in without any token in localStorage. (LF-104)
    provideAppInitializer(() => inject(AuthService).initialize()),
    provideBrowserGlobalErrorListeners(),
    provideClientHydration(withEventReplay()),
  ],
};
