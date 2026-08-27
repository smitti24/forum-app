import { z } from 'zod';

export const ProblemDetailsSchema = z.object({
  type: z.string().optional(),
  title: z.string().optional(),
  status: z.number().int().optional(),
  detail: z.string().optional(),
  instance: z.string().optional(),
  errors: z.record(z.string(), z.array(z.string())).optional(),
});

export type ProblemDetails = z.infer<typeof ProblemDetailsSchema>;

export function toProblemDetails(error: unknown): ProblemDetails | null {
  const parsed = ProblemDetailsSchema.safeParse(
    error && typeof error === 'object' && 'error' in error
      ? (error as { error: unknown }).error
      : error,
  );
  return parsed.success ? parsed.data : null;
}

export function fieldErrors(problem: ProblemDetails | null): Record<string, string[]> {
  return problem?.errors ?? {};
}
