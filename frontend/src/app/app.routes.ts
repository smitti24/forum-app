import { Routes } from '@angular/router';
import { AppShell } from './shared/components/app-shell/app-shell';
import { authRoutes } from './features/auth/auth.routes';
import { moderationRoutes } from './features/moderation/moderation.routes';
import { postsRoutes } from './features/posts/posts.routes';
import { profileRoutes } from './features/profile/profile.routes';

export const routes: Routes = [
  {
    path: '',
    component: AppShell,
    children: [...postsRoutes, ...authRoutes, ...profileRoutes, ...moderationRoutes],
  },
  { path: '**', redirectTo: '' },
];
