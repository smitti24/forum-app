import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { NbButton } from '@ng-brutalism/ui';
import { Post } from '../../../features/posts/data/post.schema';

type LikeDisabledReason = 'guest' | 'own-post' | null;

@Component({
  selector: 'app-like-button',
  imports: [NbButton],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <button
      nbButton
      size="sm"
      tone="white"
      [disabled]="reason() === 'own-post'"
      [attr.aria-label]="label()"
      [class.opacity-45]="reason() === 'own-post'"
      [class.border-dashed]="reason() === 'guest'"
      type="button"
      (click)="toggle.emit()"
    >
      {{ reason() === 'guest' ? 'Sign in' : post().likeCount }}
    </button>
  `,
})
export class LikeButton {
  readonly post = input.required<Post>();
  readonly isAuthenticated = input(false);
  readonly currentUsername = input<string | null>(null);

  readonly toggle = output<void>();

  protected readonly reason = computed<LikeDisabledReason>(() => {
    if (!this.isAuthenticated()) return 'guest';
    if (this.post().author === this.currentUsername()) return 'own-post';
    return null;
  });

  protected readonly label = computed(() =>
    this.reason() === 'own-post' ? 'You cannot like your own post' : 'Like this post',
  );
}
