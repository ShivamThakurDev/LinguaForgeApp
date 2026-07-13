import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { LearnHomeComponent } from './pages/learn-home/learn-home.component';
import { LessonMapComponent } from './pages/lesson-map/lesson-map.component';
import { LessonPlayerComponent } from './pages/lesson-player/lesson-player.component';
import { ProgressPageComponent } from './pages/progress-page/progress-page.component';
import { WelcomeComponent } from './pages/welcome/welcome.component';

export const appRoutes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'learn',
  },
  {
    path: 'welcome',
    component: WelcomeComponent,
  },
  {
    path: 'learn',
    component: LearnHomeComponent,
  },
  {
    path: 'learn/:level',
    component: LessonMapComponent,
  },
  {
    path: 'lesson-player/:lessonKey',
    component: LessonPlayerComponent,
  },
  {
    path: 'learn/:level/:lessonKey',
    component: LessonPlayerComponent,
  },
  {
    path: 'progress',
    component: ProgressPageComponent,
    canActivate: [authGuard],
  },
  {
    path: '**',
    redirectTo: 'learn',
  },
];
