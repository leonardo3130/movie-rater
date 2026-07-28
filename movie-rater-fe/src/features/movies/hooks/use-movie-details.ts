import { useQuery } from '@tanstack/react-query'
import { getMovieDetails } from '../../../api/endpoints/movies'

export function useMovieDetails(tmdbId: number | null) {
  return useQuery({
    queryKey: ['movies', tmdbId],
    queryFn: () => getMovieDetails(tmdbId!),
    enabled: tmdbId !== null,
  })
}