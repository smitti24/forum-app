import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-skeleton-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex flex-col gap-4" role="status" aria-label="Loading">
      @for (row of rows(); track $index) {
        <div class="h-28 animate-pulse border-2 border-black bg-white"></div>
      }
    </div>
  `,
})
export class SkeletonList {
  readonly count = input(3);
  protected readonly rows = () => Array.from({ length: this.count() });
}
