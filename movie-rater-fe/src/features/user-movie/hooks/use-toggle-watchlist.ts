import { useMutation, useQueryClient } from '@tanstack/react-query'
import { setWatchlist, removeWatchlist } from '../../../api/endpoints/user-movie'
import { useUserMovieStore } from '../../../stores/user-movie-store'
import { toast } from 'sonner'

export function useToggleWatchlist() {
  const queryClient = useQueryClient()
  const toggle = useUserMovieStore((s) => s.toggleWatchlist)

  return useMutation({
    mutationFn: ({ movieId, value }: { movieId: string; value: boolean }) =>
      value ? setWatchlist(movieId) : removeWatchlist(movieId),
    onMutate: ({ movieId, value }) => {
      toggle(movieId, value)
    },
    onSuccess: (data) => {
      toggle(data.movieId, data.isInWatchlist)
      queryClient.invalidateQueries({ queryKey: ['user-movie', data.movieId] })
      queryClient.invalidateQueries({ queryKey: ['user-movies'] })
    },
    onError: (_, { movieId, value }) => {
      toggle(movieId, !value)
      toast.error('Failed to update watchlist')
    },
  })
}