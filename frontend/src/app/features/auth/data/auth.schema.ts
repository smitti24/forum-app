import { z } from 'zod';
import { MemberSchema } from '../../../core/auth/member.schema';

export const USERNAME_PATTERN = /^[a-zA-Z0-9._-]{3,32}$/;

export const LoginSchema = z.object({
  identifier: z.string().min(1, 'Enter your username or email address'),
  password: z.string().min(1, 'Enter your password'),
});
export type Login = z.infer<typeof LoginSchema>;

export const RegisterSchema = z.object({
  email: z.email('Enter a valid email address'),
  username: z
    .string()
    .regex(USERNAME_PATTERN, '3-32 characters, letters, numbers, dot, dash or underscore')
    .refine((value) => !value.includes('@'), 'A username may not contain @'),
  password: z.string().min(12, 'Use at least 12 characters'),
});
export type Register = z.infer<typeof RegisterSchema>;

export const AuthResponseSchema = z.object({
  token: z.string().min(1),
  member: MemberSchema,
});
export type AuthResponse = z.infer<typeof AuthResponseSchema>;
