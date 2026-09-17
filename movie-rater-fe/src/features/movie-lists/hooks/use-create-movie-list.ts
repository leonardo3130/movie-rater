import { useMutation, useQueryClient } from '@tanstack/react-query'
import { createMovieList } from '../../../api/endpoints/movie-lists'
import type { CreateMovieListRequest } from '@src/types/movie-list'
import { toast } from 'sonner'

export function useCreateMovieList() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: CreateMovieListRequest) => createMovieList(request),
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['movie-lists'] })
      toast.success(`List "${data.name}" created`)
    },
    onError: () => {
      toast.error('Failed to create list')
    },
  })
}