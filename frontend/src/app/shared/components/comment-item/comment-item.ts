import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Comment } from '../../../features/posts/data/post.schema';
import { Avatar } from '../avatar/avatar';

@Component({
  selector: 'app-comment-item',
  imports: [DatePipe, Avatar],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex gap-[10px] p-3" [class.border-t-2]="!first()" [class.border-black]="!first()">
      <app-avatar [name]="comment().author.username" [size]="34" />

      <div class="flex min-w-0 flex-1 flex-col gap-[7px]">
        <div class="flex flex-wrap items-center gap-[7px]">
          <span class="meta">
            {{ comment().author.username }} · {{ comment().createdAt | date: 'd MMM, HH:mm' }}
          </span>
          @if (isAuthor()) {
            <span
              class="border-2 border-black px-[6px] py-[2px] font-mono text-[9.5px] font-bold uppercase"
              style="background: var(--nb-secondary)"
            >
              Author
            </span>
          }
        </div>
        <div class="text-[15px] leading-[1.5] whitespace-pre-line">{{ comment().body }}</div>
      </div>
    </div>
  `,
})
export class CommentItem {
  readonly comment = input.required<Comment>();
  readonly isAuthor = input(false);
  readonly first = input(false);
}
