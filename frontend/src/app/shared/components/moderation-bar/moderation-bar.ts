import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

@Component({
  selector: 'app-moderation-bar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="flex items-center gap-[10px] border-t-2 border-black p-3"
      style="background: var(--nb-secondary-background)"
    >
      <i class="ph-fill ph-shield-check flex-none text-[18px]"></i>
      <span class="meta flex-1">Moderator</span>
      <button
        class="press h-[38px] border-2 border-black px-3 text-[12px] font-bold tracking-[0.04em] uppercase shadow-[3px_3px_0_#000]"
        [style.background]="isFlagged() ? 'var(--nb-secondary)' : 'var(--nb-warning)'"
        [disabled]="pending()"
        type="button"
        (click)="toggle.emit()"
      >
        {{ isFlagged() ? 'Unflag' : 'Flag as misleading' }}
      </button>
    </div>
  `,
})
export class ModerationBar {
  readonly isFlagged = input(false);
  readonly pending = input(false);

  readonly toggle = output<void>();
}
