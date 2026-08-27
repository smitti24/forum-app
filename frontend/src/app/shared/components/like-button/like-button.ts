import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { Post } from '../../../features/posts/data/post.schema';

type LikeState = 'guest' | 'own-post' | 'liked' | 'rest';

@Component({
  selector: 'app-like-button',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <button
      class="flex h-[46px] flex-1 items-center justify-center gap-[7px] border-r-2 border-black text-[13px] font-bold tracking-[0.04em] uppercase"
      [style.background]="state() === 'liked' ? 'var(--nb-primary)' : '#fff'"
      [class.opacity-45]="state() === 'own-post'"
      [class.cursor-not-allowed]="state() === 'own-post'"
      [class.border-dashed]="state() === 'guest'"
      [disabled]="state() === 'own-post'"
      [attr.aria-label]="label()"
      [attr.aria-pressed]="state() === 'liked'"
      type="button"
      (click)="toggle.emit()"
    >
      <i
        class="text-[18px]"
        [class.ph-fill]="state() === 'liked'"
        [class.ph-bold]="state() !== 'liked'"
        [class.ph-heart]="true"
      ></i>
      @if (state() === 'guest') {
        Sign in
      } @else {
        <span class="tabular-nums">{{ post().likeCount }}</span>
      }
    </button>
  `,
})
export class LikeButton {
  readonly post = input.required<Post>();
  readonly isAuthenticated = input(false);
  readonly isMine = input(false);

  readonly toggle = output<void>();

  protected readonly state = computed<LikeState>(() => {
    if (!this.isAuthenticated()) return 'guest';
    if (this.isMine()) return 'own-post';
    return this.post().likedByCurrentMember ? 'liked' : 'rest';
  });

  protected readonly label = computed(() => {
    switch (this.state()) {
      case 'guest':
        return 'Sign in to like this post';
      case 'own-post':
        return 'You cannot like your own post';
      case 'liked':
        return 'Remove your like';
      default:
        return 'Like this post';
    }
  });
}
