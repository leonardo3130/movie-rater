import { z } from 'zod'

export const loginSchema = z.object({
  email: z
    .email('Invalid email address')
    .min(1, 'Email is required')
    .max(255, 'Email must be at most 255 characters'),
  password: z.string().min(1, 'Password is required'),
})

export type LoginFormValues = z.infer<typeof loginSchema>
