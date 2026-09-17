import { useMutation, useQueryClient } from '@tanstack/react-query'
import { deleteMovieList } from '../../../api/endpoints/movie-lists'
import { toast } from 'sonner'

export function useDeleteMovieList() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (listId: string) => deleteMovieList(listId),
    onSuccess: (_data, listId) => {
      queryClient.invalidateQueries({ queryKey: ['movie-lists'] })
      queryClient.removeQueries({ queryKey: ['movie-list', listId] })
      toast.success('List deleted')
    },
    onError: () => {
      toast.error('Failed to delete list')
    },
  })
}