import { z } from 'zod'

export const registerSchema = z
  .object({
    username: z
      .string()
      .min(1, 'Username is required')
      .max(100, 'Username must be at most 100 characters'),
    email:
      z
        .email("Email is not valid")
        .min(1, 'Email is required')
        .max(255, 'Email must be at most 255 characters'),
    password: z
      .string()
      .min(8, 'Password must be at least 8 characters')
      .max(100, 'Password must be at most 100 characters'),
    confirmPassword: z.string().min(1, 'Please confirm your password'),
  })
  .refine((data) => data.password === data.confirmPassword, {
    message: 'Passwords do not match',
    path: ['confirmPassword'],
  })

export type RegisterFormValues = z.infer<typeof registerSchema>
