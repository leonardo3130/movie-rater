import { useQuery } from '@tanstack/react-query'
import { getUserMovie } from '../../../api/endpoints/user-movie'

export function useUserMovie(movieId: string | undefined) {
  return useQuery({
    queryKey: ['user-movie', movieId],
    queryFn: () => getUserMovie(movieId!),
    enabled: !!movieId,
  })
}