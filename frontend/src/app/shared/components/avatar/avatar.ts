import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

const TONES = [
  'var(--nb-primary)',
  'var(--nb-secondary)',
  'var(--nb-accent)',
  'var(--nb-success)',
  'var(--nb-main)',
];

@Component({
  selector: 'app-avatar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="flex flex-none items-center justify-center border-2 border-black font-bold"
      [style.width.px]="size()"
      [style.height.px]="size()"
      [style.background]="tone()"
      [style.font-size.px]="size() * 0.38"
      aria-hidden="true"
    >
      {{ initials() }}
    </div>
  `,
})
export class Avatar {
  readonly name = input.required<string>();
  readonly size = input(38);

  protected readonly initials = computed(() => {
    const parts = this.name().split(/[.\-_\s]+/).filter(Boolean);
    const letters = parts.length > 1 ? parts[0][0] + parts[1][0] : this.name().slice(0, 2);
    return letters.toUpperCase();
  });

  protected readonly tone = computed(() => {
    const name = this.name();
    let hash = 0;
    for (let i = 0; i < name.length; i++) {
      hash = (hash * 31 + name.charCodeAt(i)) >>> 0;
    }
    return TONES[hash % TONES.length];
  });
}
