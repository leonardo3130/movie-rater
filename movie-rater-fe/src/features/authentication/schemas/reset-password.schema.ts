import { z } from 'zod'
import { t } from '@src/stores/locale-store'

export const resetPasswordSchema = z
  .object({
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

export type ResetPasswordFormValues = z.infer<typeof resetPasswordSchema>