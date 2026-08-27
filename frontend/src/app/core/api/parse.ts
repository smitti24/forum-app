import { z } from 'zod';

export class ResponseContractError extends Error {
  constructor(readonly issues: z.core.$ZodIssue[]) {
    super(`Response did not match the expected contract: ${z.prettifyError({ issues } as z.ZodError)}`);
    this.name = 'ResponseContractError';
  }
}

export function parseWith<T>(schema: z.ZodType<T>): (raw: unknown) => T {
  return (raw: unknown) => {
    const result = schema.safeParse(raw);
    if (!result.success) {
      throw new ResponseContractError(result.error.issues);
    }
    return result.data;
  };
}

export type FieldErrors = Record<string, string[] | undefined>;

export function toFieldErrors(error: z.ZodError): FieldErrors {
  return z.flattenError(error).fieldErrors as FieldErrors;
}
