import { z } from 'zod'

export const createGroupSchema = z.object({
  groupName: z.string().min(5, "Group name must be at least 5 chars").max(100, "Group name must be at most 100 chars").trim(),
})

export type CreateGroupFormValues = z.infer<typeof createGroupSchema>
