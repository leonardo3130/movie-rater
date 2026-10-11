import { z } from 'zod'
import { t } from '@src/stores/locale-store'

export const createGroupSchema = z.object({
  groupName: z
    .string()
    .min(1, { error: () => t('validation.groupNameMin') })
    .max(100, { error: () => t('validation.groupNameMax') }),
})

export type CreateGroupFormValues = z.infer<typeof createGroupSchema>