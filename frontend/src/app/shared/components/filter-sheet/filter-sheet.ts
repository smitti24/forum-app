import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  linkedSignal,
  output,
} from '@angular/core';
import { PostFilters } from '../../../features/posts/data/post.schema';

@Component({
  selector: 'app-filter-sheet',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './filter-sheet.html',
})
export class FilterSheet {
  readonly filters = input.required<PostFilters>();
  readonly total = input(0);

  readonly apply = output<Partial<PostFilters>>();
  readonly close = output<void>();

  protected readonly author = linkedSignal(() => this.filters().author ?? '');
  protected readonly from = linkedSignal(() => this.filters().from ?? '');
  protected readonly to = linkedSignal(() => this.filters().to ?? '');
  protected readonly flagged = linkedSignal<boolean | null>(() => this.filters().flagged);

  protected readonly activeCount = computed(
    () =>
      [this.author().trim(), this.from(), this.to()].filter(Boolean).length +
      (this.flagged() === null ? 0 : 1),
  );

  protected reset(): void {
    this.author.set('');
    this.from.set('');
    this.to.set('');
    this.flagged.set(null);
  }

  protected submit(): void {
    this.apply.emit({
      author: this.author().trim() || null,
      from: this.from() || null,
      to: this.to() || null,
      flagged: this.flagged(),
      page: 1,
    });
  }
}
