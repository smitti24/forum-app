import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { NbButton, NbCallout, NbSplit } from '@ng-brutalism/ui';
import { Flag } from '../../../features/posts/data/post.schema';

@Component({
  selector: 'app-moderation-bar',
  imports: [NbCallout, NbSplit, NbButton],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (flag(); as current) {
      <div nbCallout tone="warning" size="sm">
        <div nbSplit align="center" gap="sm">
          <span class="font-mono text-[11px] font-bold tracking-[0.06em] uppercase">
            Flagged as misleading by {{ current.flaggedBy }}
          </span>
          @if (isModerator()) {
            <button nbButton tone="secondary" size="sm" type="button" (click)="unflag.emit()">
              Unflag
            </button>
          }
        </div>
      </div>
    } @else if (isModerator()) {
      <button nbButton tone="warning" size="sm" type="button" (click)="flagPost.emit()">
        Flag as misleading
      </button>
    }
  `,
})
export class ModerationBar {
  readonly flag = input<Flag | null>(null);
  readonly isModerator = input(false);

  readonly flagPost = output<void>();
  readonly unflag = output<void>();
}
