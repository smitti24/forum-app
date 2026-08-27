import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { DatePipe } from '@angular/common';
import { NbStack } from '@ng-brutalism/ui';
import { Comment } from '../../../features/posts/data/post.schema';

@Component({
  selector: 'app-comment-item',
  imports: [DatePipe, NbStack],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <article nbStack gap="xs" class="border-2 border-black bg-white p-3">
      <p class="font-mono text-[11px] font-bold tracking-[0.06em] uppercase">
        {{ comment().author.username }} · {{ comment().createdAt | date: 'short' }}
      </p>
      <p class="text-[15px] font-medium whitespace-pre-line">{{ comment().body }}</p>
    </article>
  `,
})
export class CommentItem {
  readonly comment = input.required<Comment>();
}
