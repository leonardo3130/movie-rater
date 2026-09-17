import { useMutation, useQueryClient } from '@tanstack/react-query'
import { removeMovieFromList } from '../../../api/endpoints/movie-lists'
import { toast } from 'sonner'

export function useRemoveMovieFromList() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ listId, movieId }: { listId: string; movieId: string }) =>
      removeMovieFromList(listId, movieId),
    onSuccess: (_data, { listId }) => {
      queryClient.invalidateQueries({ queryKey: ['movie-lists'] })
      queryClient.invalidateQueries({ queryKey: ['movie-list', listId] })
      toast.success('Movie removed from list')
    },
    onError: () => {
      toast.error('Failed to remove movie from list')
    },
  })
}