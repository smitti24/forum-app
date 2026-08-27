import { z } from 'zod';
import { TimestampSchema } from '../../../core/auth/member.schema';
import { paged } from '../../../core/api/paged';

export const MAX_TITLE_LENGTH = 200;
export const MAX_BODY_LENGTH = 10_000;

export const PostSchema = z.object({
  id: z.uuid(),
  title: z.string().min(1),
  body: z.string(),
  author: z.string().min(1),
  createdAt: TimestampSchema,
  isFlagged: z.boolean(),
  likeCount: z.number().int().nonnegative(),
  commentCount: z.number().int().nonnegative(),
});
export type Post = z.infer<typeof PostSchema>;

export const CommentSchema = z.object({
  id: z.uuid(),
  postId: z.uuid(),
  author: z.string().min(1),
  body: z.string().min(1),
  createdAt: TimestampSchema,
});
export type Comment = z.infer<typeof CommentSchema>;

export const PagedPostsSchema = paged(PostSchema);
export const PagedCommentsSchema = paged(CommentSchema);

export const SortSchema = z.enum(['newest', 'oldest', 'most-liked']);
export type Sort = z.infer<typeof SortSchema>;

export const CreatePostSchema = z.object({
  title: z.string().trim().min(1, 'A title is required').max(MAX_TITLE_LENGTH),
  body: z.string().trim().min(1, 'A body is required').max(MAX_BODY_LENGTH),
});
export type CreatePost = z.infer<typeof CreatePostSchema>;

export const CreateCommentSchema = z.object({
  body: z.string().trim().min(1, 'A comment cannot be empty'),
});
export type CreateComment = z.infer<typeof CreateCommentSchema>;

export type PostFilters = {
  from: string | null;
  to: string | null;
  author: string | null;
  flagged: boolean | null;
  sort: Sort;
  page: number;
  pageSize: number;
};

export const DEFAULT_FILTERS: PostFilters = {
  from: null,
  to: null,
  author: null,
  flagged: null,
  sort: 'newest',
  page: 1,
  pageSize: 20,
};
