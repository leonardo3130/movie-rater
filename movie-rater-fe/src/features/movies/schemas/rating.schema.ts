import { z } from 'zod'

export const createRatingSchema = z.object({
  ratingValue: z.number().int().min(1, 'Rating must be at least 1').max(10, 'Rating must be at most 10'),
  review: z.string().max(5000, 'Review must be at most 5000 characters').optional().or(z.literal('')),
})

export type CreateRatingFormValues = z.infer<typeof createRatingSchema>