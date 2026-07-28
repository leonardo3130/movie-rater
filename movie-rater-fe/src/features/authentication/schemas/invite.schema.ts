import { z } from 'zod'

export const inviteSchema = z.object({
  inviteeEmail: z.string().email('Please enter a valid email address'),
})

export type InviteFormValues = z.infer<typeof inviteSchema>