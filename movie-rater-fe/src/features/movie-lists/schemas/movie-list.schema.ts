import { z } from 'zod'
import { t } from '@src/stores/locale-store'

export const movieListSchema = z
  .object({
    name: z
      .string()
      .min(1, { error: () => t('validation.nameRequired') })
      .max(100, { error: () => t('validation.nameMax') }),
    description: z
      .string()
      .max(500, { error: () => t('validation.descriptionMax') })
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
        message: t('validation.privateListCannotEdit'),
      })
    }
    if (!data.isPrivate && data.groupIds.length === 0) {
      ctx.addIssue({
        code: 'custom',
        path: ['groupIds'],
        message: t('validation.selectAtLeastOneGroup'),
      })
    }
  })

export type MovieListFormValues = z.infer<typeof movieListSchema>

export const quickListSchema = z.object({
  name: z
    .string()
    .min(1, { error: () => t('validation.nameRequired') })
    .max(100, { error: () => t('validation.nameMax') }),
})

export type QuickListFormValues = z.infer<typeof quickListSchema>