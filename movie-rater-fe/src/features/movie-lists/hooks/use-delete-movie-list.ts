import { useMutation, useQueryClient } from '@tanstack/react-query'
import { deleteMovieList } from '../../../api/endpoints/movie-lists'
import { toast } from 'sonner'
import { t } from '@src/stores/locale-store'

export function useDeleteMovieList() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (listId: string) => deleteMovieList(listId),
    onSuccess: (_data, listId) => {
      queryClient.invalidateQueries({ queryKey: ['movie-lists'] })
      queryClient.removeQueries({ queryKey: ['movie-list', listId] })
      toast.success(t('lists.toast.deleted'))
    },
    onError: () => {
      toast.error(t('lists.toast.deleteFailed'))
    },
  })
}