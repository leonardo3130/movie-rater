import { z } from 'zod'

export const createGroupSchema = z.object({
  groupName: z.string().min(1, "Group name must be at least 1 chars").max(100, "Group name must be at most 100 chars"),
})

export type CreateGroupFormValues = z.infer<typeof createGroupSchema>
