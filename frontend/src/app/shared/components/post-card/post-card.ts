import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Post } from '../../../features/posts/data/post.schema';
import { Avatar } from '../avatar/avatar';
import { LikeButton } from '../like-button/like-button';

@Component({
  selector: 'app-post-card',
  imports: [DatePipe, RouterLink, Avatar, LikeButton],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './post-card.html',
})
export class PostCard {
  readonly post = input.required<Post>();
  readonly isAuthenticated = input(false);
  readonly currentMemberId = input<string | null>(null);
  readonly dense = input(true);

  readonly toggleLike = output<void>();

  protected readonly isMine = computed(() => this.post().author.id === this.currentMemberId());
}
