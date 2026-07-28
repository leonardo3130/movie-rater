import { useQuery } from '@tanstack/react-query'
import { getUserMovies } from '../../../api/endpoints/user-movie'
import { useUserMovieStore } from '../../../stores/user-movie-store'
import { useEffect } from 'react'

export function useWatchlist(page = 1, pageSize = 20) {
  const setBatch = useUserMovieStore((s) => s.setBatch)

  const query = useQuery({
    queryKey: ['user-movies', 'watchlist', page, pageSize],
    queryFn: () =>
      getUserMovies({ watchlistOnly: true, page, pageSize }).then((res) => ({
        ...res,
        results: res.results.map((r) => ({
          ...r,
          isInWatchlist: true,
        })),
      })),
  })

  useEffect(() => {
    if (query.data) {
      setBatch(
        query.data.results.map((r) => ({ id: r.id, isFavorite: r.isFavorite, isInWatchlist: true })),
      )
    }
  }, [query.data, setBatch])

  return query
}