import { Routes } from '@angular/router';
import { LearnHomeComponent } from './pages/learn-home/learn-home.component';
import { LessonMapComponent } from './pages/lesson-map/lesson-map.component';
import { LessonPlayerComponent } from './pages/lesson-player/lesson-player.component';
import { ProgressPageComponent } from './pages/progress-page/progress-page.component';

export const appRoutes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'learn',
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
  },
  {
    path: '**',
    redirectTo: 'learn',
  },
];
