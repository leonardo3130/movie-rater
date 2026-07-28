import { useQuery } from '@tanstack/react-query'
import { searchMovies } from '../../../api/endpoints/movies'

export function useSearchMovies(query: string, page: number) {
  return useQuery({
    queryKey: ['movies', 'search', query, page],
    queryFn: () => searchMovies({ query, page }),
    enabled: query.trim().length > 0,
  })
}