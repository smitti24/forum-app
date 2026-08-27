import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { NbButton, NbCluster, NbSplit, NbSurface } from '@ng-brutalism/ui';
import { AuthStore } from '../../../core/auth/auth-store';

@Component({
  selector: 'app-app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, NbSurface, NbCluster, NbSplit, NbButton],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './app-shell.html',
})
export class AppShell {
  protected readonly auth = inject(AuthStore);

  protected logout(): void {
    this.auth.signOut();
  }
}
