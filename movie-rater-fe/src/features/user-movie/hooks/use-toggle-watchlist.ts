import { useMutation, useQueryClient } from '@tanstack/react-query'
import {
  setWatchlist,
  removeWatchlist,
  setWatchlistByTmdb,
  removeWatchlistByTmdb,
} from '../../../api/endpoints/user-movie'
import { useUserMovieStore } from '../../../stores/user-movie-store'
import { toast } from 'sonner'

function watchlistMutation({ movieId, value }: { movieId: string; value: boolean }) {
  const isTmdb = !movieId.includes('-')
  return isTmdb
    ? value
      ? setWatchlistByTmdb(Number(movieId))
      : removeWatchlistByTmdb(Number(movieId))
    : value
      ? setWatchlist(movieId)
      : removeWatchlist(movieId)
}

export function useToggleWatchlist() {
  const queryClient = useQueryClient()
  const toggle = useUserMovieStore((s) => s.toggleWatchlist)

  return useMutation({
    mutationFn: watchlistMutation,
    onMutate: ({ movieId, value }) => {
      toggle(movieId, value)
    },
    onSuccess: (_data, { movieId, value }) => {
      toggle(movieId, value)
      queryClient.invalidateQueries({ queryKey: ['user-movie', movieId] })
      queryClient.invalidateQueries({ queryKey: ['user-movies'] })
      queryClient.invalidateQueries({ queryKey: ['movies'] })
    },
    onError: (_, { movieId, value }) => {
      toggle(movieId, !value)
      toast.error('Failed to update watchlist')
    },
  })
}