import { z } from 'zod'

export const inviteSchema = z.object({
  groupId: z.guid().nonoptional('Please selecta  group'),
  inviteeUsername: z
    .string()
    .min(1, "Please enter your partner's username")
    .max(100, 'Username is too long')
    .nonoptional(),
})

export type InviteFormValues = z.infer<typeof inviteSchema>