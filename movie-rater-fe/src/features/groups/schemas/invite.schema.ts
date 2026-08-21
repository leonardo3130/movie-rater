import { z } from 'zod'

export const inviteSchema = z.object({
  groupId: z.guid().nonoptional(),
  inviteeEmail: z.email('Please enter a valid email address').nonoptional(),
})

export type InviteFormValues = z.infer<typeof inviteSchema>
