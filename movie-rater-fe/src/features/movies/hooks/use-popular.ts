import { useQuery } from '@tanstack/react-query'
import { getPopularMovies } from '../../../api/endpoints/movies'

export function usePopular(page: number) {
  return useQuery({
    queryKey: ['movies', 'popular', page],
    queryFn: () => getPopularMovies({ page }),
  })
}