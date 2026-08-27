import { z } from 'zod';
import { RoleSchema, TimestampSchema } from '../../../core/auth/member.schema';

export const MIN_PASSWORD_LENGTH = 12;
export const USERNAME_PATTERN = /^[A-Za-z0-9._-]{3,32}$/;

export const LoginSchema = z.object({
  identifier: z.string().min(1, 'Enter your username or email address'),
  password: z.string().min(1, 'Enter your password'),
});
export type Login = z.infer<typeof LoginSchema>;

export const RegisterSchema = z.object({
  email: z.email('That does not look like an email address').max(320),
  username: z
    .string()
    .regex(USERNAME_PATTERN, 'Between 3 and 32 characters, using only letters, digits, and . _ -'),
  password: z
    .string()
    .min(MIN_PASSWORD_LENGTH, `Use at least ${MIN_PASSWORD_LENGTH} characters. Length matters more than symbols.`),
});
export type Register = z.infer<typeof RegisterSchema>;

export const TokenResponseSchema = z.object({
  accessToken: z.string().min(1),
  expiresAt: TimestampSchema,
  username: z.string().min(1),
  role: RoleSchema,
});
export type TokenResponse = z.infer<typeof TokenResponseSchema>;

export const RegisteredResponseSchema = z.object({
  id: z.uuid(),
  username: z.string().min(1),
});
