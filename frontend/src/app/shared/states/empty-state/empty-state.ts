import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-empty-state',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex flex-col items-center gap-3 border-2 border-black bg-white p-10 text-center">
      <p class="font-mono text-[12px] font-bold tracking-[0.06em] uppercase">{{ headline() }}</p>
      <p class="text-[15px] font-medium">{{ message() }}</p>
      <ng-content />
    </div>
  `,
})
export class EmptyState {
  readonly headline = input.required<string>();
  readonly message = input('');
}
