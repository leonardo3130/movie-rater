import { z } from 'zod'

export const createWatchSessionSchema = z.object({
  watchedAt: z.string().min(1, 'Date is required'),
  location: z.string().max(200, 'Location must be at most 200 characters').optional().or(z.literal('')),
  notes: z.string().max(2000, 'Notes must be at most 2000 characters').optional().or(z.literal('')),
})

export type CreateWatchSessionFormValues = z.infer<typeof createWatchSessionSchema>