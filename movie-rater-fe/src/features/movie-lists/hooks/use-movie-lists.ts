import { useQuery } from '@tanstack/react-query'
import { getMovieLists } from '../../../api/endpoints/movie-lists'

export function useMovieLists() {
  return useQuery({
    queryKey: ['movie-lists'],
    queryFn: getMovieLists,
  })
}