import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PostsApi } from '../../../posts/data/posts-api';
import { DEFAULT_FILTERS } from '../../../posts/data/post.schema';
import { SkeletonList } from '../../../../shared/states/skeleton-list/skeleton-list';
import { EmptyState } from '../../../../shared/states/empty-state/empty-state';
import { ErrorState } from '../../../../shared/states/error-state/error-state';

@Component({
  selector: 'app-moderation-page',
  imports: [RouterLink, SkeletonList, EmptyState, ErrorState],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './moderation-page.html',
})
export class ModerationPage {
  private readonly api = inject(PostsApi);

  private readonly flaggedFilters = signal({ ...DEFAULT_FILTERS, flagged: true });
  private readonly allFilters = signal({ ...DEFAULT_FILTERS, pageSize: 1 });

  protected readonly flagged = this.api.postsResource(this.flaggedFilters);
  protected readonly all = this.api.postsResource(this.allFilters);

  protected readonly flaggedCount = computed(() => this.flagged.value()?.total ?? 0);
  protected readonly totalCount = computed(() => this.all.value()?.total ?? 0);

  protected readonly pending = signal<string | null>(null);

  protected async unflag(postId: string): Promise<void> {
    this.pending.set(postId);
    try {
      await this.api.unflag(postId);
      this.flagged.reload();
    } finally {
      this.pending.set(null);
    }
  }
}
