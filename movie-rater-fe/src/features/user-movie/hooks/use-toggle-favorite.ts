import { useMutation, useQueryClient } from '@tanstack/react-query'
import {
  setFavorite,
  removeFavorite,
  setFavoriteByTmdb,
  removeFavoriteByTmdb,
} from '../../../api/endpoints/user-movie'
import { useUserMovieStore } from '../../../stores/user-movie-store'
import { toast } from 'sonner'

function favoriteMutation({ movieId, value }: { movieId: string; value: boolean }) {
  const isTmdb = !movieId.includes('-')
  return isTmdb
    ? value
      ? setFavoriteByTmdb(Number(movieId))
      : removeFavoriteByTmdb(Number(movieId))
    : value
      ? setFavorite(movieId)
      : removeFavorite(movieId)
}

export function useToggleFavorite() {
  const queryClient = useQueryClient()
  const toggle = useUserMovieStore((s) => s.toggleFavorite)

  return useMutation({
    mutationFn: favoriteMutation,
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
      toast.error('Failed to update favorite')
    },
  })
}