import { z } from 'zod'
import { t } from '@src/stores/locale-store'

export const registerSchema = z
  .object({
    username: z
      .string()
      .min(1, { error: () => t('validation.usernameRequired') })
      .max(100, { error: () => t('validation.usernameMax') }),
    email: z
      .email({ error: () => t('validation.invalidEmail') })
      .min(1, { error: () => t('validation.emailRequired') })
      .max(255, { error: () => t('validation.emailMax') }),
    password: z
      .string()
      .min(8, { error: () => t('validation.passwordMin') })
      .max(100, { error: () => t('validation.passwordMax') }),
    confirmPassword: z
      .string()
      .min(1, { error: () => t('validation.confirmPasswordRequired') }),
  })
  .refine((data) => data.password === data.confirmPassword, {
    error: () => t('validation.passwordsMismatch'),
    path: ['confirmPassword'],
  })

export type RegisterFormValues = z.infer<typeof registerSchema>