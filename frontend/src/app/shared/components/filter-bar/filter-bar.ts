import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { NbButton, NbCluster, NbInput, NbNativeSelect } from '@ng-brutalism/ui';
import { PostFilters, Sort } from '../../../features/posts/data/post.schema';

@Component({
  selector: 'app-filter-bar',
  imports: [NbCluster, NbInput, NbNativeSelect, NbButton],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './filter-bar.html',
})
export class FilterBar {
  readonly filters = input.required<PostFilters>();
  readonly total = input(0);

  readonly filtersChange = output<Partial<PostFilters>>();
  readonly clear = output<void>();

  protected readonly activeCount = computed(() => {
    const { from, to, author, flagged } = this.filters();
    return [from, to, author, flagged].filter((value) => value !== null).length;
  });

  protected patch(change: Partial<PostFilters>): void {
    this.filtersChange.emit({ ...change, page: 1 });
  }

  protected onSort(value: string): void {
    this.patch({ sort: value as Sort });
  }

  protected onFlagged(value: string): void {
    this.patch({ flagged: value === '' ? null : value === 'true' });
  }
}
