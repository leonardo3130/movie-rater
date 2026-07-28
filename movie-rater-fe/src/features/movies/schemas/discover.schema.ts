import { z } from 'zod'

const sortByOptions = [
  'popularity.desc',
  'popularity.asc',
  'vote_average.desc',
  'vote_average.asc',
  'primary_release_date.desc',
  'primary_release_date.asc',
  'revenue.desc',
  'revenue.asc',
  'original_title.asc',
  'original_title.desc',
] as const

export const discoverSchema = z.object({
  genreIds: z.array(z.number()).default([]),
  primaryReleaseYear: z.string().regex(/^\d{4}$/).optional().or(z.literal('')),
  primaryReleaseDateGte: z.string().optional().or(z.literal('')),
  primaryReleaseDateLte: z.string().optional().or(z.literal('')),
  voteAverageGte: z.number().min(0).max(10).default(0),
  sortBy: z.enum(sortByOptions).default('popularity.desc'),
  includeAdult: z.boolean().default(false),
})

export type DiscoverFormValues = z.infer<typeof discoverSchema>