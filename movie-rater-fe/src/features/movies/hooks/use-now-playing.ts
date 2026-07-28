import { useQuery } from '@tanstack/react-query'
import { getNowPlayingMovies } from '../../../api/endpoints/movies'

export function useNowPlaying(page: number) {
  return useQuery({
    queryKey: ['movies', 'now-playing', page],
    queryFn: () => getNowPlayingMovies({ page }),
  })
}