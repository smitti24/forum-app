import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { NbCluster, NbSplit, NbStack, NbSurface } from '@ng-brutalism/ui';
import { Post } from '../../../features/posts/data/post.schema';
import { LikeButton } from '../like-button/like-button';
import { ModerationBar } from '../moderation-bar/moderation-bar';

@Component({
  selector: 'app-post-card',
  imports: [DatePipe, RouterLink, NbSurface, NbStack, NbCluster, NbSplit, LikeButton, ModerationBar],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './post-card.html',
})
export class PostCard {
  readonly post = input.required<Post>();
  readonly isAuthenticated = input(false);
  readonly isModerator = input(false);
  readonly currentUsername = input<string | null>(null);
  readonly linkToDetail = input(true);

  readonly toggleLike = output<void>();
  readonly flagPost = output<void>();
  readonly unflag = output<void>();
}
