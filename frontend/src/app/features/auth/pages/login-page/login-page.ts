import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { NbButton, NbInput, NbStack } from '@ng-brutalism/ui';
import { AuthApi } from '../../data/auth-api';
import { LoginSchema } from '../../data/auth.schema';
import { FieldErrors, toFieldErrors } from '../../../../core/api/parse';

@Component({
  selector: 'app-login-page',
  imports: [NbStack, NbInput, NbButton],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './login-page.html',
})
export class LoginPage {
  private readonly api = inject(AuthApi);
  private readonly router = inject(Router);

  readonly returnUrl = signal('/');

  protected readonly identifier = signal('');
  protected readonly password = signal('');
  protected readonly errors = signal<FieldErrors>({});
  protected readonly submitting = signal(false);

  protected async submit(): Promise<void> {
    const parsed = LoginSchema.safeParse({
      identifier: this.identifier(),
      password: this.password(),
    });
    if (!parsed.success) {
      this.errors.set(toFieldErrors(parsed.error));
      return;
    }

    this.errors.set({});
    this.submitting.set(true);
    try {
      await this.api.login(parsed.data);
      await this.router.navigateByUrl(this.returnUrl());
    } catch {
      this.errors.set({ form: ['Those credentials were not recognised.'] });
    } finally {
      this.submitting.set(false);
    }
  }
}
