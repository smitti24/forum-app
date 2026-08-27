import { Routes } from '@angular/router';
import { moderatorGuard } from '../../core/auth/moderator-guard';

export const moderationRoutes: Routes = [
  {
    path: 'moderation',
    title: 'Moderation',
    canActivate: [moderatorGuard],
    loadComponent: () =>
      import('./pages/moderation-page/moderation-page').then((m) => m.ModerationPage),
  },
];
