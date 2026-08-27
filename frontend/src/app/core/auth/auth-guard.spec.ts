import { TestBed } from '@angular/core/testing';
import { Router, UrlTree } from '@angular/router';
import { provideRouter } from '@angular/router';
import { authGuard } from './auth-guard';
import { moderatorGuard } from './moderator-guard';
import { AuthStore } from './auth-store';
import { Member } from './member.schema';

const moderator: Member = {
  id: '0198f2c1-4a3b-7c8d-9e0f-1a2b3c4d5e6f',
  username: 'mod',
  email: 'mod@example.com',
  role: 'moderator',
};

function run(guard: typeof authGuard, url = '/posts/new') {
  return TestBed.runInInjectionContext(() =>
    guard({} as never, { url } as never),
  );
}

describe('route guards', () => {
  let store: AuthStore;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideRouter([])] });
    store = TestBed.inject(AuthStore);
  });

  it('redirects an unauthenticated caller to the login page, keeping the return url', () => {
    const result = run(authGuard) as UrlTree;

    expect(result).toBeInstanceOf(UrlTree);
    expect(TestBed.inject(Router).serializeUrl(result)).toContain('/login');
    expect(TestBed.inject(Router).serializeUrl(result)).toContain('returnUrl');
  });

  it('admits an authenticated caller', () => {
    store.setToken('a-token', '2026-08-27T08:00:00Z');

    expect(run(authGuard)).toBe(true);
  });

  it('refuses a member the moderation route', () => {
    store.setToken('a-token', '2026-08-27T08:00:00Z');
    store.setMember({ ...moderator, role: 'member' });

    expect(run(moderatorGuard, '/moderation')).toBeInstanceOf(UrlTree);
  });

  it('admits a moderator', () => {
    store.setToken('a-token', '2026-08-27T08:00:00Z');
    store.setMember(moderator);

    expect(run(moderatorGuard, '/moderation')).toBe(true);
  });
});
