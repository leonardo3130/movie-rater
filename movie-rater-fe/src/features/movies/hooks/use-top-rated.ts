import { useQuery } from '@tanstack/react-query'
import { getTopRatedMovies } from '../../../api/endpoints/movies'

export function useTopRated(page: number) {
  return useQuery({
    queryKey: ['movies', 'top-rated', page],
    queryFn: () => getTopRatedMovies({ page }),
  })
}