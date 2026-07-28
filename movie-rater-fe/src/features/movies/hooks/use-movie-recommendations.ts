import { useQuery } from '@tanstack/react-query'
import { getMovieRecommendations } from '../../../api/endpoints/movies'

export function useMovieRecommendations(tmdbId: number | null) {
  return useQuery({
    queryKey: ['movies', tmdbId, 'recommendations'],
    queryFn: () => getMovieRecommendations(tmdbId!),
    enabled: tmdbId !== null,
  })
}