import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

@Component({
  selector: 'app-skeleton-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex flex-col gap-[14px]" role="status" aria-label="Loading">
      @for (row of rows(); track $index) {
        <div
          class="flex flex-col gap-3 border-2 border-black bg-white p-[14px] shadow-[4px_4px_0_#000]"
        >
          <div class="flex items-center gap-[10px]">
            <div class="h-[38px] w-[38px] border-2 border-black" [style]="shimmer(0)"></div>
            <div class="flex flex-1 flex-col gap-[6px]">
              <div class="h-[12px] w-[46%] border-2 border-black" [style]="shimmer(0.1)"></div>
              <div class="h-[10px] w-[28%] border-2 border-black" [style]="shimmer(0.2)"></div>
            </div>
          </div>
          <div class="h-[14px] w-[88%] border-2 border-black" [style]="shimmer(0.15)"></div>
          <div class="h-[12px] w-full border-2 border-black" [style]="shimmer(0.25)"></div>
        </div>
      }
    </div>
  `,
})
export class SkeletonList {
  readonly count = input(3);

  protected readonly rows = computed(() => Array.from({ length: this.count() }));

  protected shimmer(delay: number): string {
    return `background: var(--nb-secondary-background); animation: nbshimmer 1.3s ease-in-out ${delay}s infinite`;
  }
}
