import { z } from 'zod'
import { t } from '@src/stores/locale-store'

export const inviteSchema = z.object({
  groupId: z.guid().nonoptional({ error: () => t('validation.selectGroupRequired') }),
  inviteeUsername: z
    .string()
    .min(1, { error: () => t('validation.partnerUsernameRequired') })
    .max(100, { error: () => t('validation.usernameTooLong') })
    .nonoptional(),
})

export type InviteFormValues = z.infer<typeof inviteSchema>