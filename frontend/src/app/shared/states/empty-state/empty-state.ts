import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-empty-state',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="flex flex-col items-start gap-3 border-2 border-black bg-white px-4 py-[22px] shadow-[4px_4px_0_#000]"
    >
      <div
        class="flex h-[44px] w-[44px] items-center justify-center border-2 border-black"
        [style.background]="tone()"
      >
        <i [class]="icon()" class="text-[22px]"></i>
      </div>
      <div class="text-[17px] font-bold uppercase">{{ headline() }}</div>
      @if (message()) {
        <div class="text-[14px] leading-[1.5]">{{ message() }}</div>
      }
      <ng-content />
    </div>
  `,
})
export class EmptyState {
  readonly headline = input.required<string>();
  readonly message = input('');
  readonly icon = input('ph-bold ph-chats-circle');
  readonly tone = input('var(--nb-accent)');
}
