import { z } from 'zod'
import { t } from '@src/stores/locale-store'

export const createRatingSchema = z.object({
  ratingValue: z
    .number()
    .int()
    .min(1, { error: () => t('validation.ratingMin') })
    .max(10, { error: () => t('validation.ratingMax') }),
  review: z
    .string()
    .max(5000, { error: () => t('validation.reviewMax') })
    .optional()
    .or(z.literal('')),
})

export type CreateRatingFormValues = z.infer<typeof createRatingSchema>