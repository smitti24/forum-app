import { HttpClient } from '@angular/common/http';
import { Injectable, Signal, inject } from '@angular/core';
import { httpResource } from '@angular/common/http';
import { firstValueFrom, map } from 'rxjs';
import { API_BASE_URL } from '../../../core/api/api.config';
import { parseWith } from '../../../core/api/parse';
import {
  CreateComment,
  CreatePost,
  PagedCommentsSchema,
  PagedPostsSchema,
  PostDetailSchema,
  PostFilters,
  PostSchema,
} from './post.schema';

function toParams(filters: PostFilters): Record<string, string | number | boolean> {
  const params: Record<string, string | number | boolean> = {
    sort: filters.sort,
    page: filters.page,
    pageSize: filters.pageSize,
  };
  if (filters.from) params['from'] = filters.from;
  if (filters.to) params['to'] = filters.to;
  if (filters.author) params['author'] = filters.author;
  if (filters.flagged !== null) params['flagged'] = filters.flagged;
  return params;
}

@Injectable({ providedIn: 'root' })
export class PostsApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  postsResource(filters: Signal<PostFilters>) {
    return httpResource(
      () => ({ url: `${this.baseUrl}/posts`, params: toParams(filters()) }),
      { parse: parseWith(PagedPostsSchema) },
    );
  }

  postResource(postId: Signal<string | null>) {
    return httpResource(
      () => {
        const id = postId();
        return id ? `${this.baseUrl}/posts/${id}` : undefined;
      },
      { parse: parseWith(PostDetailSchema) },
    );
  }

  commentsResource(postId: Signal<string | null>, page: Signal<number>) {
    return httpResource(
      () => {
        const id = postId();
        return id
          ? { url: `${this.baseUrl}/posts/${id}/comments`, params: { page: page() } }
          : undefined;
      },
      { parse: parseWith(PagedCommentsSchema) },
    );
  }

  createPost(input: CreatePost) {
    return firstValueFrom(
      this.http
        .post<unknown>(`${this.baseUrl}/posts`, input)
        .pipe(map(parseWith(PostSchema))),
    );
  }

  createComment(postId: string, input: CreateComment) {
    return firstValueFrom(
      this.http.post<unknown>(`${this.baseUrl}/posts/${postId}/comments`, input),
    );
  }

  like(postId: string) {
    return firstValueFrom(this.http.post<unknown>(`${this.baseUrl}/posts/${postId}/like`, {}));
  }

  unlike(postId: string) {
    return firstValueFrom(this.http.delete<unknown>(`${this.baseUrl}/posts/${postId}/like`));
  }

  flag(postId: string) {
    return firstValueFrom(this.http.post<unknown>(`${this.baseUrl}/posts/${postId}/flag`, {}));
  }

  unflag(postId: string) {
    return firstValueFrom(this.http.delete<unknown>(`${this.baseUrl}/posts/${postId}/flag`));
  }
}
