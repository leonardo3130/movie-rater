import { useMutation, useQueryClient } from '@tanstack/react-query'
import { removeMovieFromList } from '../../../api/endpoints/movie-lists'
import { toast } from 'sonner'
import { t } from '@src/stores/locale-store'
import { getApiErrorMessage } from '@src/api/errors'

export function useRemoveMovieFromList() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ listId, movieId }: { listId: string; movieId: string }) =>
      removeMovieFromList(listId, movieId),
    onSuccess: (_data, { listId }) => {
      queryClient.invalidateQueries({ queryKey: ['movie-lists'] })
      queryClient.invalidateQueries({ queryKey: ['movie-list', listId] })
      toast.success(t('lists.toast.removed'))
    },
    onError: (error: unknown) => {
      toast.error(getApiErrorMessage(error, t('lists.toast.removeFailed')))
    },
  })
}