import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { NbButton, NbInput, NbStack, NbTextarea } from '@ng-brutalism/ui';
import { PostsApi } from '../../data/posts-api';
import { CreatePostSchema } from '../../data/post.schema';
import { FieldErrors, toFieldErrors } from '../../../../core/api/parse';
import { toProblemDetails } from '../../../../core/api/problem-details';

@Component({
  selector: 'app-create-post-page',
  imports: [NbStack, NbInput, NbTextarea, NbButton],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './create-post-page.html',
})
export class CreatePostPage {
  private readonly api = inject(PostsApi);
  private readonly router = inject(Router);

  protected readonly title = signal('');
  protected readonly body = signal('');
  protected readonly errors = signal<FieldErrors>({});
  protected readonly submitting = signal(false);

  protected async submit(): Promise<void> {
    const parsed = CreatePostSchema.safeParse({ title: this.title(), body: this.body() });
    if (!parsed.success) {
      this.errors.set(toFieldErrors(parsed.error));
      return;
    }

    this.errors.set({});
    this.submitting.set(true);
    try {
      const post = await this.api.createPost(parsed.data);
      await this.router.navigate(['/posts', post.id]);
    } catch (error) {
      this.errors.set(toProblemDetails(error)?.errors ?? { form: ['The post could not be created.'] });
    } finally {
      this.submitting.set(false);
    }
  }
}
