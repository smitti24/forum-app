import { Routes } from '@angular/router';
import { authGuard } from '../../core/auth/auth-guard';

export const profileRoutes: Routes = [
  {
    path: 'profile',
    title: 'Profile',
    canActivate: [authGuard],
    loadComponent: () => import('./pages/profile-page/profile-page').then((m) => m.ProfilePage),
  },
];
