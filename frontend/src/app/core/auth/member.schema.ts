import { z } from 'zod';

export const RoleSchema = z.enum(['member', 'moderator']);
export type Role = z.infer<typeof RoleSchema>;

export const AuthorSchema = z.object({
  id: z.uuid(),
  username: z.string().min(1),
});
export type Author = z.infer<typeof AuthorSchema>;

export const MemberSchema = z.object({
  id: z.uuid(),
  username: z.string().min(1),
  email: z.email(),
  role: RoleSchema,
});
export type Member = z.infer<typeof MemberSchema>;
