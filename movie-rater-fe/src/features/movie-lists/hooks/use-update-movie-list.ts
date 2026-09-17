import { useMutation, useQueryClient } from '@tanstack/react-query'
import { updateMovieList } from '../../../api/endpoints/movie-lists'
import type { UpdateMovieListRequest } from '@src/types/movie-list'
import { toast } from 'sonner'

export function useUpdateMovieList() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ listId, request }: { listId: string; request: UpdateMovieListRequest }) =>
      updateMovieList(listId, request),
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['movie-lists'] })
      queryClient.invalidateQueries({ queryKey: ['movie-list', data.id] })
      toast.success(`List "${data.name}" updated`)
    },
    onError: () => {
      toast.error('Failed to update list')
    },
  })
}