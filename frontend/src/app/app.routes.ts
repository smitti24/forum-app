import { Routes } from '@angular/router';
import { AppShell } from './shared/components/app-shell/app-shell';
import { authRoutes } from './features/auth/auth.routes';
import { postsRoutes } from './features/posts/posts.routes';

export const routes: Routes = [
  {
    path: '',
    component: AppShell,
    children: [...postsRoutes, ...authRoutes],
  },
  { path: '**', redirectTo: '' },
];
