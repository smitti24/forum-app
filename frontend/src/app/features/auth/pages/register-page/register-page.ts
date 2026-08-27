import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AuthApi } from '../../data/auth-api';
import { RegisterSchema } from '../../data/auth.schema';
import { FieldErrors, toFieldErrors } from '../../../../core/api/parse';

@Component({
  selector: 'app-register-page',
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './register-page.html',
})
export class RegisterPage {
  private readonly api = inject(AuthApi);
  private readonly router = inject(Router);

  protected readonly email = signal('');
  protected readonly username = signal('');
  protected readonly password = signal('');
  protected readonly errors = signal<FieldErrors>({});
  protected readonly submitting = signal(false);

  protected async submit(): Promise<void> {
    const parsed = RegisterSchema.safeParse({
      email: this.email(),
      username: this.username(),
      password: this.password(),
    });
    if (!parsed.success) {
      this.errors.set(toFieldErrors(parsed.error));
      return;
    }

    this.errors.set({});
    this.submitting.set(true);
    try {
      await this.api.register(parsed.data);
      await this.router.navigateByUrl('/');
    } catch {
      this.errors.set({ form: ['That username or email address is already taken.'] });
    } finally {
      this.submitting.set(false);
    }
  }
}
