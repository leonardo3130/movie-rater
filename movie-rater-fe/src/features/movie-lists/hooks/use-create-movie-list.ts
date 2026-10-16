import { useMutation, useQueryClient } from '@tanstack/react-query'
import { createMovieList } from '../../../api/endpoints/movie-lists'
import type { CreateMovieListRequest } from '@src/types/movie-list'
import { toast } from 'sonner'
import { t } from '@src/stores/locale-store'
import { getApiErrorMessage } from '@src/api/errors'

export function useCreateMovieList() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: CreateMovieListRequest) => createMovieList(request),
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['movie-lists'] })
      toast.success(t('lists.toast.created', { name: data.name }))
    },
    onError: (error: unknown) => {
      toast.error(getApiErrorMessage(error, t('lists.toast.createFailed')))
    },
  })
}