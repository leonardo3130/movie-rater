import { useQuery } from '@tanstack/react-query'
import { getMovieList } from '../../../api/endpoints/movie-lists'

export function useMovieList(listId: string | undefined) {
  return useQuery({
    queryKey: ['movie-list', listId],
    queryFn: () => getMovieList(listId!),
    enabled: !!listId,
  })
}