import { useQuery } from '@tanstack/react-query'
import { getUserMovies } from '../../../api/endpoints/user-movie'
import { useUserMovieStore } from '../../../stores/user-movie-store'
import { useEffect } from 'react'

export function useFavorites(page = 1, pageSize = 20) {
  const setBatch = useUserMovieStore((s) => s.setBatch)

  const query = useQuery({
    queryKey: ['user-movies', 'favorites', page, pageSize],
    queryFn: () =>
      getUserMovies({ favoritesOnly: true, page, pageSize }).then((res) => ({
        ...res,
        results: res.results.map((r) => ({
          ...r,
          isFavorite: true,
          tmdbId: r.tmdbId,
        })),
      })),
  })

  useEffect(() => {
    if (query.data) {
      setBatch(
        query.data.results.map((r) => ({ id: r.id, isFavorite: true, isInWatchlist: r.isInWatchlist })),
      )
    }
  }, [query.data, setBatch])

  return query
}