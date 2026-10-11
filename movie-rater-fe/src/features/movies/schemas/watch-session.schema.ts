import { z } from 'zod'
import { t } from '@src/stores/locale-store'

export const createWatchSessionSchema = z.object({
  groupId: z.guid({ error: () => t('validation.groupIdRequired') }),
  watchedAt: z.string().min(1, { error: () => t('validation.dateRequired') }),
  location: z
    .string()
    .max(200, { error: () => t('validation.locationMax') })
    .optional()
    .or(z.literal('')),
  notes: z
    .string()
    .max(2000, { error: () => t('validation.notesMax') })
    .optional()
    .or(z.literal('')),
})

export type CreateWatchSessionFormValues = z.infer<typeof createWatchSessionSchema>

export const updateWatchSessionSchema = z.object({
  watchedAt: z.string().min(1, { error: () => t('validation.dateRequired') }),
  location: z
    .string()
    .max(200, { error: () => t('validation.locationMax') })
    .optional()
    .or(z.literal('')),
  notes: z
    .string()
    .max(2000, { error: () => t('validation.notesMax') })
    .optional()
    .or(z.literal('')),
})

export type UpdateWatchSessionFormValues = z.infer<typeof updateWatchSessionSchema>