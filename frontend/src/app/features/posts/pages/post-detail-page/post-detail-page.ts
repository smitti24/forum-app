import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { NbButton, NbTextarea } from '@ng-brutalism/ui';
import { AuthStore } from '../../../../core/auth/auth-store';
import { PostsApi } from '../../data/posts-api';
import { PostCard } from '../../../../shared/components/post-card/post-card';
import { CommentItem } from '../../../../shared/components/comment-item/comment-item';
import { SkeletonList } from '../../../../shared/states/skeleton-list/skeleton-list';
import { EmptyState } from '../../../../shared/states/empty-state/empty-state';
import { ErrorState } from '../../../../shared/states/error-state/error-state';

@Component({
  selector: 'app-post-detail-page',
  imports: [NbButton, NbTextarea, PostCard, CommentItem, SkeletonList, EmptyState, ErrorState],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './post-detail-page.html',
})
export class PostDetailPage {
  private readonly api = inject(PostsApi);
  protected readonly auth = inject(AuthStore);

  readonly id = input.required<string>();

  private readonly postId = computed(() => this.id());
  protected readonly commentPage = signal(1);

  protected readonly post = this.api.postResource(this.postId);
  protected readonly comments = this.api.commentsResource(this.postId, this.commentPage);

  protected readonly draft = signal('');
  protected readonly submitting = signal(false);

  protected async addComment(): Promise<void> {
    const body = this.draft().trim();
    if (!body) return;

    this.submitting.set(true);
    try {
      await this.api.createComment(this.id(), { body });
      this.draft.set('');
      this.comments.reload();
      this.post.reload();
    } finally {
      this.submitting.set(false);
    }
  }

  protected async toggleLike(liked: boolean): Promise<void> {
    await (liked ? this.api.unlike(this.id()) : this.api.like(this.id()));
    this.post.reload();
  }

  protected async toggleFlag(flagged: boolean): Promise<void> {
    await (flagged ? this.api.unflag(this.id()) : this.api.flag(this.id()));
    this.post.reload();
  }
}
