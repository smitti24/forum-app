import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { FeedPage } from './feed-page';

function post(overrides: Record<string, unknown> = {}) {
  return {
    id: '0198f2c1-4a3b-7c8d-9e0f-1a2b3c4d5e70',
    title: 'Integrating the SDK',
    body: 'Body text.',
    author: { id: '0198f2c1-4a3b-7c8d-9e0f-1a2b3c4d5e6f', username: 'asmith' },
    createdAt: '2026-08-27T06:31:46Z',
    likeCount: 0,
    commentCount: 0,
    likedByCurrentMember: false,
    flag: null,
    ...overrides,
  };
}

describe('FeedPage states', () => {
  let controller: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    controller = TestBed.inject(HttpTestingController);
  });

  function render() {
    const fixture = TestBed.createComponent(FeedPage);
    fixture.detectChanges();
    return fixture;
  }

  it('renders the loading state before the feed resolves', async () => {
    const fixture = TestBed.createComponent(FeedPage);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('[role="status"]')).toBeTruthy();
  });

  it('renders the empty state when no post matches', async () => {
    const fixture = render();

    controller.expectOne((r) => r.url === '/api/v1/posts').flush({
      items: [],
      page: 1,
      pageSize: 20,
      total: 0,
    });
    await fixture.whenStable();

    expect(fixture.nativeElement.textContent).toContain('Nothing here');
  });

  it('renders the error state when the request fails', async () => {
    const fixture = render();

    controller
      .expectOne((r) => r.url === '/api/v1/posts')
      .flush('boom', { status: 503, statusText: 'Service Unavailable' });
    await fixture.whenStable();

    expect(fixture.nativeElement.textContent).toContain('Feed failed');
  });

  it('renders the error state when a response breaks the contract', async () => {
    const fixture = render();

    controller.expectOne((r) => r.url === '/api/v1/posts').flush({
      items: [post({ likeCount: 'many' })],
      page: 1,
      pageSize: 20,
      total: 1,
    });
    await fixture.whenStable();

    expect(fixture.nativeElement.textContent).toContain('Feed failed');
  });

  it('renders a populated feed', async () => {
    const fixture = render();

    controller.expectOne((r) => r.url === '/api/v1/posts').flush({
      items: [post()],
      page: 1,
      pageSize: 20,
      total: 1,
    });
    await fixture.whenStable();

    expect(fixture.nativeElement.textContent).toContain('Integrating the SDK');
    expect(fixture.nativeElement.textContent).toContain('Reading as a visitor');
  });
});
