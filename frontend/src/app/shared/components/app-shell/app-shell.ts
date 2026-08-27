import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterOutlet } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { filter, map, startWith } from 'rxjs';
import { AuthStore } from '../../../core/auth/auth-store';
import { Avatar } from '../avatar/avatar';

type Tab = { path: string; icon: string; label: string; moderatorOnly?: boolean };

const TABS: Tab[] = [
  { path: '/', icon: 'ph-fill ph-house', label: 'Feed' },
  { path: '/moderation', icon: 'ph-bold ph-shield-check', label: 'Mod', moderatorOnly: true },
  { path: '/profile', icon: 'ph-bold ph-user', label: 'You' },
];

@Component({
  selector: 'app-app-shell',
  imports: [RouterOutlet, RouterLink, Avatar],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './app-shell.html',
})
export class AppShell {
  private readonly router = inject(Router);
  protected readonly auth = inject(AuthStore);

  protected readonly url = toSignal(
    this.router.events.pipe(
      filter((event) => event instanceof NavigationEnd),
      map((event) => event.urlAfterRedirects),
      startWith(this.router.url),
    ),
    { initialValue: this.router.url },
  );

  protected readonly tabs = computed(() =>
    TABS.filter((tab) => !tab.moderatorOnly || this.auth.isModerator()),
  );

  protected readonly title = computed(() => {
    const url = this.url();
    if (url.startsWith('/login')) return 'Sign in';
    if (url.startsWith('/register')) return 'Register';
    if (url.startsWith('/posts/new')) return 'New post';
    if (url.startsWith('/posts/')) return 'Post';
    if (url.startsWith('/moderation')) return 'Moderation';
    if (url.startsWith('/profile')) return 'Profile';
    return 'Forum';
  });

  protected readonly canGoBack = computed(() => this.url() !== '/');

  protected readonly showNav = computed(
    () => !this.url().startsWith('/login') && !this.url().startsWith('/register'),
  );

  protected isActive(path: string): boolean {
    return path === '/' ? this.url() === '/' : this.url().startsWith(path);
  }

  protected back(): void {
    this.canGoBack() ? history.back() : void this.router.navigate(['/']);
  }
}
