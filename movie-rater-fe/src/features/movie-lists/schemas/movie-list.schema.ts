import { z } from 'zod'

export const movieListSchema = z
  .object({
    name: z.string().min(1, 'Name is required').max(100, 'Name must be at most 100 characters'),
    description: z
      .string()
      .max(500, 'Description must be at most 500 characters')
      .optional(),
    isPrivate: z.boolean(),
    canGroupEdit: z.boolean(),
    groupIds: z.array(z.string()),
  })
  .superRefine((data, ctx) => {
    if (data.canGroupEdit && data.isPrivate) {
      ctx.addIssue({
        code: 'custom',
        path: ['canGroupEdit'],
        message: 'A private list cannot be edited by group members.',
      })
    }
    if (!data.isPrivate && data.groupIds.length === 0) {
      ctx.addIssue({
        code: 'custom',
        path: ['groupIds'],
        message: 'Select at least one group to share the list with.',
      })
    }
  })

export type MovieListFormValues = z.infer<typeof movieListSchema>

export const quickListSchema = z.object({
  name: z.string().min(1, 'Name is required').max(100, 'Name must be at most 100 characters'),
})

export type QuickListFormValues = z.infer<typeof quickListSchema>