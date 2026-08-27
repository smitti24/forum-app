import { Routes } from '@angular/router';
import { authGuard } from '../../core/auth/auth-guard';

export const postsRoutes: Routes = [
  {
    path: '',
    title: 'Forum',
    loadComponent: () => import('./pages/feed-page/feed-page').then((m) => m.FeedPage),
  },
  {
    path: 'posts/new',
    title: 'New post',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./pages/create-post-page/create-post-page').then((m) => m.CreatePostPage),
  },
  {
    path: 'posts/:id',
    title: 'Post',
    loadComponent: () =>
      import('./pages/post-detail-page/post-detail-page').then((m) => m.PostDetailPage),
  },
];
