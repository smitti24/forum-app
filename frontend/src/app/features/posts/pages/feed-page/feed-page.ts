import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { AuthStore } from '../../../../core/auth/auth-store';
import { PostsApi } from '../../data/posts-api';
import { DEFAULT_FILTERS, PostFilters, Sort } from '../../data/post.schema';
import { PostCard } from '../../../../shared/components/post-card/post-card';
import { FilterSheet } from '../../../../shared/components/filter-sheet/filter-sheet';
import { SkeletonList } from '../../../../shared/states/skeleton-list/skeleton-list';
import { EmptyState } from '../../../../shared/states/empty-state/empty-state';
import { ErrorState } from '../../../../shared/states/error-state/error-state';

const SORTS: { key: Sort; label: string }[] = [
  { key: 'newest', label: 'Newest' },
  { key: 'oldest', label: 'Oldest' },
  { key: 'most-liked', label: 'Top' },
];

@Component({
  selector: 'app-feed-page',
  imports: [PostCard, FilterSheet, SkeletonList, EmptyState, ErrorState],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './feed-page.html',
})
export class FeedPage {
  private readonly api = inject(PostsApi);
  protected readonly auth = inject(AuthStore);

  protected readonly sorts = SORTS;
  protected readonly filters = signal<PostFilters>(DEFAULT_FILTERS);
  protected readonly sheetOpen = signal(false);
  protected readonly posts = this.api.postsResource(this.filters);

  protected readonly total = computed(() => this.posts.value()?.total ?? 0);

  protected readonly activeFilterCount = computed(() => {
    const { author, from, to, flagged } = this.filters();
    return [author, from, to].filter(Boolean).length + (flagged === null ? 0 : 1);
  });

  protected patch(change: Partial<PostFilters>): void {
    this.filters.update((current) => ({ ...current, ...change }));
  }

  protected applyFilters(change: Partial<PostFilters>): void {
    this.patch(change);
    this.sheetOpen.set(false);
  }

  protected clearFilters(): void {
    this.filters.set(DEFAULT_FILTERS);
  }

  protected async toggleLike(postId: string, liked: boolean): Promise<void> {
    await (liked ? this.api.unlike(postId) : this.api.like(postId));
    this.posts.reload();
  }
}
