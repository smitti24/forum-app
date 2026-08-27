import { z } from 'zod';

export function paged<T extends z.ZodType>(item: T) {
  return z.object({
    items: z.array(item),
    page: z.number().int().positive(),
    pageSize: z.number().int().positive(),
    total: z.number().int().nonnegative(),
  });
}

export type Paged<T> = {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
};
