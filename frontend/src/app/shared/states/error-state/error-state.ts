import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

@Component({
  selector: 'app-error-state',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="flex flex-col items-start gap-3 border-2 border-black bg-white px-4 py-[22px] shadow-[4px_4px_0_#000]"
      role="alert"
    >
      <div
        class="flex h-[44px] w-[44px] items-center justify-center border-2 border-black"
        style="background: var(--nb-danger)"
      >
        <i class="ph-bold ph-cloud-slash text-[22px]"></i>
      </div>
      <div class="text-[17px] font-bold uppercase">{{ headline() }}</div>
      <div class="text-[14px] leading-[1.5]">{{ message() }}</div>
      <button
        class="press h-[42px] border-2 border-black px-[14px] text-[12px] font-bold tracking-[0.04em] uppercase shadow-[3px_3px_0_#000]"
        style="background: var(--nb-primary)"
        type="button"
        (click)="retry.emit()"
      >
        Try again
      </button>
    </div>
  `,
})
export class ErrorState {
  readonly headline = input('Request failed');
  readonly message = input('The forum could not be reached. Nothing you wrote was lost.');
  readonly retry = output<void>();
}
