import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { authInterceptor } from './auth-interceptor';
import { AuthStore } from './auth-store';
import { Member } from './member.schema';

const member: Member = {
  id: '0198f2c1-4a3b-7c8d-9e0f-1a2b3c4d5e6f',
  username: 'asmith',
  email: 'asmith@example.com',
  role: 'member',
};

describe('authInterceptor', () => {
  let http: HttpClient;
  let controller: HttpTestingController;
  let store: AuthStore;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
      ],
    });

    http = TestBed.inject(HttpClient);
    controller = TestBed.inject(HttpTestingController);
    store = TestBed.inject(AuthStore);
  });

  afterEach(() => controller.verify());

  it('attaches the token to an API request once signed in', () => {
    store.setToken('a-token', '2026-08-27T08:00:00Z');
    store.setMember(member);

    http.get('/api/v1/posts').subscribe();

    const request = controller.expectOne('/api/v1/posts');
    expect(request.request.headers.get('Authorization')).toBe('Bearer a-token');
  });

  it('sends no Authorization header while signed out', () => {
    http.get('/api/v1/posts').subscribe();

    const request = controller.expectOne('/api/v1/posts');
    expect(request.request.headers.has('Authorization')).toBe(false);
  });

  it('does not leak the token to a non-API host', () => {
    store.setToken('a-token', '2026-08-27T08:00:00Z');

    http.get('https://example.com/track').subscribe();

    const request = controller.expectOne('https://example.com/track');
    expect(request.request.headers.has('Authorization')).toBe(false);
  });
});
