import { z } from 'zod';

export const TimestampSchema = z.iso.datetime({ offset: true, local: true });

export const RoleSchema = z.enum(['Member', 'Moderator']);
export type Role = z.infer<typeof RoleSchema>;

export const MemberSchema = z.object({
  id: z.uuid(),
  username: z.string().min(1),
  email: z.email(),
  role: RoleSchema,
  createdAt: TimestampSchema,
});
export type Member = z.infer<typeof MemberSchema>;
