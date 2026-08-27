import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { NbButton } from '@ng-brutalism/ui';

@Component({
  selector: 'app-error-state',
  imports: [NbButton],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="flex flex-col items-center gap-3 border-2 border-[var(--nb-danger)] bg-white p-10 text-center"
      role="alert"
    >
      <p class="font-mono text-[12px] font-bold tracking-[0.06em] uppercase">Something went wrong</p>
      <p class="text-[15px] font-medium">{{ message() }}</p>
      <button nbButton tone="danger" size="sm" type="button" (click)="retry.emit()">Try again</button>
    </div>
  `,
})
export class ErrorState {
  readonly message = input('The forum could not be reached.');
  readonly retry = output<void>();
}
