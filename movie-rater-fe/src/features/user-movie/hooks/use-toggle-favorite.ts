import { useMutation, useQueryClient } from '@tanstack/react-query'
import { setFavorite, removeFavorite } from '../../../api/endpoints/user-movie'
import { useUserMovieStore } from '../../../stores/user-movie-store'
import { toast } from 'sonner'

export function useToggleFavorite() {
  const queryClient = useQueryClient()
  const toggle = useUserMovieStore((s) => s.toggleFavorite)

  return useMutation({
    mutationFn: ({ movieId, value }: { movieId: string; value: boolean }) =>
      value ? setFavorite(movieId) : removeFavorite(movieId),
    onMutate: ({ movieId, value }) => {
      toggle(movieId, value)
    },
    onSuccess: (data) => {
      toggle(data.movieId, data.isFavorite)
      queryClient.invalidateQueries({ queryKey: ['user-movie', data.movieId] })
      queryClient.invalidateQueries({ queryKey: ['user-movies'] })
    },
    onError: (_, { movieId, value }) => {
      toggle(movieId, !value)
      toast.error('Failed to update favorite')
    },
  })
}