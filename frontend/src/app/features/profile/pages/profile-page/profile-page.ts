import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AuthApi } from '../../../auth/data/auth-api';
import { AuthStore } from '../../../../core/auth/auth-store';
import { PostsApi } from '../../../posts/data/posts-api';
import { DEFAULT_FILTERS } from '../../../posts/data/post.schema';
import { Avatar } from '../../../../shared/components/avatar/avatar';
import { SkeletonList } from '../../../../shared/states/skeleton-list/skeleton-list';
import { EmptyState } from '../../../../shared/states/empty-state/empty-state';
import { ErrorState } from '../../../../shared/states/error-state/error-state';

@Component({
  selector: 'app-profile-page',
  imports: [RouterLink, Avatar, SkeletonList, EmptyState, ErrorState],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './profile-page.html',
})
export class ProfilePage {
  private readonly api = inject(PostsApi);
  private readonly authApi = inject(AuthApi);
  private readonly router = inject(Router);
  protected readonly auth = inject(AuthStore);

  private readonly filters = signal({
    ...DEFAULT_FILTERS,
    author: this.auth.username(),
  });

  protected readonly posts = this.api.postsResource(this.filters);
  protected readonly total = computed(() => this.posts.value()?.total ?? 0);

  protected signOut(): void {
    this.authApi.logout();
    void this.router.navigate(['/']);
  }
}
