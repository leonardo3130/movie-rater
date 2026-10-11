import { z } from 'zod'
import { t } from '@src/stores/locale-store'

export const loginSchema = z.object({
  email: z
    .email({ error: () => t('validation.invalidEmail') })
    .min(1, { error: () => t('validation.emailRequired') })
    .max(255, { error: () => t('validation.emailMax') }),
  password: z.string().min(1, { error: () => t('validation.passwordRequired') }),
})

export type LoginFormValues = z.infer<typeof loginSchema>