import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { AuthStore } from '../../../../core/auth/auth-store';
import { PostsApi } from '../../data/posts-api';
import { DEFAULT_FILTERS, PostFilters } from '../../data/post.schema';
import { PostCard } from '../../../../shared/components/post-card/post-card';
import { FilterBar } from '../../../../shared/components/filter-bar/filter-bar';
import { SkeletonList } from '../../../../shared/states/skeleton-list/skeleton-list';
import { EmptyState } from '../../../../shared/states/empty-state/empty-state';
import { ErrorState } from '../../../../shared/states/error-state/error-state';

@Component({
  selector: 'app-feed-page',
  imports: [PostCard, FilterBar, SkeletonList, EmptyState, ErrorState],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './feed-page.html',
})
export class FeedPage {
  private readonly api = inject(PostsApi);
  protected readonly auth = inject(AuthStore);

  protected readonly filters = signal<PostFilters>(DEFAULT_FILTERS);
  protected readonly posts = this.api.postsResource(this.filters);

  protected patchFilters(change: Partial<PostFilters>): void {
    this.filters.update((current) => ({ ...current, ...change }));
  }

  protected clearFilters(): void {
    this.filters.set(DEFAULT_FILTERS);
  }

  protected async toggleLike(postId: string, liked: boolean): Promise<void> {
    await (liked ? this.api.unlike(postId) : this.api.like(postId));
    this.posts.reload();
  }

  protected async toggleFlag(postId: string, flagged: boolean): Promise<void> {
    await (flagged ? this.api.unflag(postId) : this.api.flag(postId));
    this.posts.reload();
  }
}
